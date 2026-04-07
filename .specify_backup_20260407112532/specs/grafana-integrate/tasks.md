# Tasks: Grafana Integration

## Implementation Strategy
MVP = US1 (Loki sink wired up) → US2 (structured business events) → US3 (PII exclusion test).
Each phase is independently verifiable. US2 and US3 can proceed in parallel after US1.

---

## Phase 1: Setup

- [x] T001 Add `Serilog.Sinks.Grafana.Loki` NuGet package to `Backend/src/Host/CopyTradeMarketApi.Host/CopyTradeMarketApi.Host.csproj`
- [x] T002 Add `Serilog.Sinks.TestCorrelator` NuGet package to `Backend/tests/Tracking.Application.Tests/Tracking.Application.Tests.csproj`

---

## Phase 2: Foundational

- [x] T003 Add `Loki:Uri`, `Loki:Username`, and `Loki:Password` config keys with `"SET_VIA_USER_SECRETS_OR_ENV"` placeholders to `Backend/src/Host/CopyTradeMarketApi.Host/appsettings.json`
- [x] T004 Inject `ILogger<TrackingService>` into `TrackingService` via primary constructor in `Backend/src/Modules/Tracking/Tracking.Application/Services/TrackingService.cs`

---

## Phase 3: US1 — Ship Application Logs to Loki

**Goal**: All Serilog log entries are forwarded to Loki asynchronously. Loki endpoint is config-driven.

- [x] T005 [US1] Add Loki sink entry to the `"WriteTo"` array in `appsettings.json` with `uri`, `username`, `password` from config, `batchPostingLimit: 1000`, `queueLimit: 10000` in `Backend/src/Host/CopyTradeMarketApi.Host/appsettings.json`
- [x] T006 [US1] Verify app starts and logs flow to console/file with Loki sink present but `Loki:Uri` unset — confirm no startup crash

---

## Phase 4: US2 — Emit Structured Business Event Properties

**Goal**: Click and conversion log entries carry `EventType`, `AffiliateCode`, `SessionId`, and `ConversionType` as structured properties.

- [x] T007 [US2] Add `Log.Information` call with `{EventType}`, `{AffiliateCode}`, `{SessionId}` properties after `SaveChangesAsync` in `RecordClickAsync` in `Backend/src/Modules/Tracking/Tracking.Application/Services/TrackingService.cs`
- [x] T008 [US2] Add `Log.Information` call with `{EventType}`, `{AffiliateCode}`, `{SessionId}`, `{ConversionType}` properties after `SaveChangesAsync` in `RecordConversionAsync` in `Backend/src/Modules/Tracking/Tracking.Application/Services/TrackingService.cs`

---

## Phase 5: US3 — Sensitive Field Exclusion

**Goal**: A passing unit test proves `IpAddress`, `Email`, and `UserAgent` are absent from all log events at `Information` level.

- [x] T009 [US3] Create `Backend/tests/Tracking.Application.Tests/LokiLogSchemaTests.cs` with tests using `TestCorrelator` asserting `IpAddress` is absent from click log events
- [x] T010 [P] [US3] Add assertion in `LokiLogSchemaTests.cs` that `UserAgent` is absent from click log events in `Backend/tests/Tracking.Application.Tests/LokiLogSchemaTests.cs`
- [x] T011 [P] [US3] Add assertion in `LokiLogSchemaTests.cs` that `Email` is absent from auth-related log events in `Backend/tests/Tracking.Application.Tests/LokiLogSchemaTests.cs`
- [x] T012 [P] [US3] Add assertion in `LokiLogSchemaTests.cs` that `EventType`, `AffiliateCode`, `SessionId` ARE present on click log events (positive contract test) in `Backend/tests/Tracking.Application.Tests/LokiLogSchemaTests.cs`

---

## Final Phase: Polish

- [x] T013 Run full test suite (`dotnet test`) and confirm all tests pass including new `LokiLogSchemaTests`
- [ ] T014 Confirm `appsettings.json` contains no real Loki credentials — only `"SET_VIA_USER_SECRETS_OR_ENV"` placeholders

---

## Dependencies

```
T001 → T005 → T006
T002 → T009 → T010, T011, T012 (parallel)
T003 → T005
T004 → T007, T008
T007, T008 → T012
```

US2 and US3 phases can proceed in parallel once T004 (logger injection) is done.

---

## Summary

| Phase | Story | Tasks |
|---|---|---|
| Setup | — | T001, T002 |
| Foundational | — | T003, T004 |
| Phase 3 | US1 | T005, T006 |
| Phase 4 | US2 | T007, T008 |
| Phase 5 | US3 | T009, T010, T011, T012 |
| Polish | — | T013, T014 |
| **Total** | | **14 tasks** |

**MVP scope**: T001 → T003 → T005 → T006 (Loki sink wired and verified).  
**Parallel opportunities**: T010, T011, T012 (all independent test assertions in same file).
