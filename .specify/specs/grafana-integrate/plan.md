# Implementation Plan: Grafana Integration

## Technical Context

### Tech Stack
- .NET 8 / ASP.NET Core — existing host
- Serilog — already configured in `Program.cs` with console + file sinks
- `Serilog.Sinks.Grafana.Loki` — new NuGet sink to ship logs to Loki
- Grafana Loki — log aggregation backend (new Docker service)
- Grafana — dashboard/alerting UI (new Docker service)
- Docker Compose — existing orchestration extended with Loki + Grafana services

### Architecture Approach
- **Log pipeline**: `Serilog` (in-process) → Loki HTTP sink (async, fire-and-forget) → Grafana queries Loki
- **No new modules**: Grafana integration lives entirely in the Host project (Serilog config) and Docker Compose. It does not require a new IModule.
- **No new DB entities**: Loki is the store for log data. No EF migrations needed.
- **Business metrics via structured logs**: Clicks and conversions are already domain events; enrich log events at event handler call sites with structured properties (`AffiliateCode`, `ConversionType`, `SessionId`) so Loki/Grafana can derive metrics via LogQL.
- **Dashboards as code**: Grafana dashboards provisioned via JSON files mounted into the Grafana container — kept in `infra/grafana/dashboards/`.

### Constitution Check
- [x] **P1 — Modules Are Islands**: No inter-module references introduced. Serilog enrichment happens at existing event handler boundaries, not by coupling modules.
- [x] **P2 — Specification Pattern**: Not applicable (no new DB queries).
- [x] **P3 — Domain Events for Side Effects**: Business metric logs are emitted at domain event handler call sites (e.g., `UserRegisteredEventHandler`, click tracking), not by cross-module direct calls.
- [x] **P4 — Secrets Never In Source**: Loki URL and Grafana admin password provided via environment variables / Docker Compose env, not hardcoded.
- [x] **P5 — Async All the Way**: Loki sink is configured in batched async mode (`queueLimit`, `batchPostingLimit`) — never blocks request pipeline.
- [x] **P6 — Consistent Error Contract**: Not applicable to log shipping. ExceptionHandlingMiddleware already logs errors via Serilog before returning ProblemDetails.

---

## Phase 0: Research

### Unknowns to Resolve
- Alert notification channel: email, Slack webhook, or PagerDuty? *(Open — default to Slack webhook placeholder in provisioning config)*
- Dashboard management: confirmed as code (JSON in repo).
- Grafana hosting: confirmed as Docker Compose alongside the API.

### Decisions Made

| Decision | Choice | Rationale | Alternatives |
|---|---|---|---|
| Log transport | `Serilog.Sinks.Grafana.Loki` | Fits existing Serilog pipeline; no agent needed | Promtail sidecar (more ops overhead), direct HTTP to Loki (reinventing the sink) |
| Business metrics source | Structured log properties on existing domain event handlers | No new infrastructure; LogQL can aggregate by label | Prometheus counters (requires new `/metrics` endpoint, out of scope) |
| Dashboard provisioning | JSON files in `infra/grafana/dashboards/` | Version-controlled, reproducible | Grafana UI (manual, not reproducible) |
| Grafana access control | Grafana built-in auth (admin account only); not exposed on public port | Simplest for internal/Docker use | OAuth, LDAP (over-engineering for current stage) |
| Sensitive field filtering | Destructure policy + log level gate in Serilog config | P4 compliance; no IP/email at Info+ | Custom sink filter |

---

## Phase 1: Design

### Data Model
No new database entities. See [data-model.md](data-model.md) for the Loki log schema (structured log field contracts).

### Interface Contracts
See [contracts/loki-log-schema.md](contracts/loki-log-schema.md) for the structured log event contract.

### Project Structure

```
CopyTradeMarket/
├── Backend/
│   ├── src/
│   │   └── Host/
│   │       └── CopyTradeMarketApi.Host/
│   │           └── appsettings.json              # Add Loki sink config (URL via env var)
│   └── docker-compose.yml                        # Add loki + grafana services
│
└── infra/
    └── grafana/
        ├── provisioning/
        │   ├── datasources/
        │   │   └── loki.yml                      # Auto-provision Loki datasource
        │   └── dashboards/
        │       └── dashboards.yml                # Dashboard discovery config
        └── dashboards/
            ├── business-metrics.json             # Clicks + conversions dashboard
            └── system-health.json                # HTTP metrics + error rate dashboard
```

---

## Dependencies

| Package / Service | Version | Purpose |
|---|---|---|
| `Serilog.Sinks.Grafana.Loki` | latest stable | Ships Serilog log events to Loki |
| `Serilog.Enrichers.Environment` | already referenced | `MachineName` enrichment (already in use) |
| Grafana Loki | `grafana/loki:2.9` | Log aggregation backend |
| Grafana | `grafana/grafana:10.4` | Dashboard + alerting UI |

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Loki sink blocks request thread under back-pressure | Low | High | Configure `queueLimit` + `batchPostingLimit`; sink drops on overflow |
| Sensitive data (email/IP) leaks into Loki | Medium | High | Serilog destructure policy + `MinimumLevel.Override` for sensitive loggers; verified in integration test |
| Grafana container not accessible in CI | Low | Low | Grafana/Loki are Docker Compose optional profile; CI tests do not depend on them |
| Dashboard JSON drift from actual log schema | Medium | Medium | Document log field contract in `contracts/loki-log-schema.md`; update both together |
| Loki disk usage grows unbounded | Low | Medium | Set `retention_period: 30d` in Loki config |
