# Implementation Plan: Grafana Integration

## Technical Context

### Tech Stack
- .NET 8 / ASP.NET Core — existing host
- Serilog — already configured in `Program.cs` (console + file sinks, `appsettings.json`-driven)
- `Serilog.Sinks.Grafana.Loki` — new NuGet package, adds a Loki HTTP sink
- `Serilog.Sinks.TestCorrelator` — new NuGet package (test only), captures log events in-memory for assertions

### Architecture Approach
- **Change surface is minimal**: only `appsettings.json` (Loki sink config) and two call sites in `TrackingService.cs` (structured log properties on click/conversion results)
- **No new module, no new DbContext, no new migrations**
- Loki sink is added to the existing `"WriteTo"` array in `appsettings.json` — endpoint URL read from environment variable via `"Args": { "uri": "#{Loki__Uri}#" }` pattern, or directly from config key `Loki:Uri`
- Business event log properties are emitted in `TrackingService.RecordClickAsync` and `RecordConversionAsync` after successful DB writes — structured properties only, no PII

### Constitution Check
- [x] **P1 — Modules Are Islands**: No inter-module references. Serilog is host-level infrastructure; `TrackingService` adds log calls within its own boundary.
- [x] **P2 — Specification Pattern**: Not applicable — no new DB queries.
- [x] **P3 — Domain Events for Side Effects**: Not applicable — logging is a local side effect, not a cross-module concern.
- [x] **P4 — Secrets Never In Source**: Loki URI provided via `Loki:Uri` config key, sourced from environment variable. Never hardcoded. `appsettings.json` uses placeholder `"SET_VIA_USER_SECRETS_OR_ENV"`.
- [x] **P5 — Async All the Way**: Loki sink configured with `batchPostingLimit` and `queueLimit` — fire-and-forget, does not block request thread.
- [x] **P6 — Consistent Error Contract**: Not applicable to log shipping. `ExceptionHandlingMiddleware` already logs errors via Serilog before returning ProblemDetails.

---

## Phase 0: Research

### Unknowns to Resolve
- Loki endpoint URL format for the target environment — placeholder used; must be supplied via env var at deploy time.

### Decisions Made

| Decision | Choice | Rationale | Alternatives |
|---|---|---|---|
| Loki sink package | `Serilog.Sinks.Grafana.Loki` | Native Serilog integration; no agent sidecar needed | Promtail sidecar (more ops overhead), raw HTTP (reinventing the sink) |
| Loki URI config | `Loki:Uri` in `appsettings.json` with env var override | Consistent with existing config pattern (P4) | Hardcoded (violates P4) |
| Business event log placement | After successful `db.SaveChangesAsync()` in `TrackingService` | Ensures log only fires on confirmed writes; no phantom events | Before save (risks logging events that didn't persist) |
| PII exclusion mechanism | Structured log properties never include `IpAddress`, `Email`, `UserAgent` — only derived/safe fields | Simplest approach; no destructure policy needed if properties are simply never passed | Serilog destructure policy (more complex, same result) |
| Test approach | `Serilog.Sinks.TestCorrelator` in unit tests | In-process, fast, no Loki instance needed | Integration test hitting real Loki (slow, infra dependency) |

---

## Phase 1: Design

### Data Model
No new database entities. See [data-model.md](data-model.md) for the structured log event field contract.

### Interface Contracts
See [contracts/loki-log-schema.md](contracts/loki-log-schema.md) for the log event schema Loki consumers depend on.

### Project Structure

Only two files change in production code; one test file is added:

```
Backend/
├── src/
│   └── Host/
│       └── CopyTradeMarketApi.Host/
│           └── appsettings.json                        # ADD Loki sink to "WriteTo" array
│
│   └── Modules/
│       └── Tracking/
│           └── Tracking.Application/
│               └── Services/
│                   └── TrackingService.cs              # ADD structured log calls after SaveChangesAsync
│
└── tests/
    └── Tracking.Application.Tests/
        └── LokiLogSchemaTests.cs                       # NEW — assert structured props + PII exclusion
```

---

## Dependencies

| Package | Project | Purpose |
|---|---|---|
| `Serilog.Sinks.Grafana.Loki` | `CopyTradeMarketApi.Host` | Ships log events to Loki over HTTP |
| `Serilog.Sinks.TestCorrelator` | `Tracking.Application.Tests` | Captures Serilog events in-memory for unit test assertions |

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Loki URI not set in environment → sink silently drops or throws | Medium | Low | Sink configured with `handleLogLevelRestrictions: false`; app starts normally if Loki unreachable |
| PII field accidentally added to log call | Low | High | Unit test (`LokiLogSchemaTests`) asserts absence of `IpAddress`, `Email`, `UserAgent` — fails build if violated |
| Loki sink blocks request under back-pressure | Low | High | `queueLimit` set; sink drops events on overflow rather than blocking |
| `appsettings.json` Loki URI accidentally committed with real value | Low | Medium | Placeholder value `"SET_VIA_USER_SECRETS_OR_ENV"` committed; real value only in env |
