# Tasks: API Version Exposure

## Implementation Strategy

Config and code first (T001–T005) → CI workflow last (T006–T007). The endpoint works immediately with the local fallback value; the CI workflow enables automatic SHA injection on every main build.

---

## Phase 1: Configuration — US3

**Goal**: `ApiVersion` fallback values are committed so the app always starts with a valid version string.

- [x] T001 [US3] Add `"ApiVersion": "0.0.0-local"` at the top level of `Backend/src/Host/CopyTradeMarketApi.Host/appsettings.json`
- [x] T002 [US3] Add `"ApiVersion": "0.0.0-dev"` at the top level of `Backend/src/Host/CopyTradeMarketApi.Host/appsettings.Development.json`
- [x] T003 [US3] Add `ApiVersion: "${ApiVersion:-0.0.0-local}"` to the `api` service `environment` block in `Backend/docker-compose.yml`

---

## Phase 2: API Endpoint & Swagger — US1, US2

**Goal**: `GET /api/version` exists and Swagger reflects the same value.

- [x] T004 [US2] Read `builder.Configuration["ApiVersion"] ?? "0.0.0-local"` into a local `apiVersion` variable immediately before `builder.Services.AddSwaggerGen` in `Backend/src/Host/CopyTradeMarketApi.Host/Program.cs`
- [x] T005 [US2] Replace the hardcoded `Version = "v1"` inside `SwaggerDoc("v1", ...)` with `Version = apiVersion` in `Program.cs`
- [x] T006 [US1] Add `app.MapGet("/api/version", () => Results.Ok(new { version = apiVersion })).WithTags("Meta").AllowAnonymous()` after `app.MapControllers()` in `Program.cs`

---

## Phase 3: CI Workflow — US4

**Goal**: Every push to `main` runs tests and starts the container with `ApiVersion` set to the short git SHA.

- [ ] T007 [US4] Create `.github/workflows/ci.yml` — SHA step added; deploy step pending (no prod machine)

---

## Final Phase: Verification

- [ ] T008 Run `docker compose up --build api` locally and confirm `GET /api/version` returns `{ "version": "0.0.0-dev" }` in Development environment
- [ ] T009 Confirm `GET /swagger/v1/swagger.json` `info.version` matches the value from `GET /api/version`
- [ ] T010 Set `ApiVersion=test-sha-123` in local shell, restart container, confirm `GET /api/version` returns `{ "version": "test-sha-123" }` — proves env var override works
- [ ] T011 Push to main branch and confirm GitHub Actions injects the short SHA — `GET /api/version` on the deployed instance returns a 7-char hex string

---

## Dependencies

```
T001, T002, T003 (parallel) → T004 → T005, T006 (parallel)
T006 → T008, T009, T010 (parallel verification)
T007 is independent of T001–T006 (file creation only)
T011 requires T007 + all prior tasks merged to main
```

---

## Summary

| Phase | Story | Tasks |
|---|---|---|
| Phase 1 — Config | US3 | T001, T002, T003 |
| Phase 2 — Endpoint & Swagger | US1, US2 | T004, T005, T006 |
| Phase 3 — CI Workflow | US4 | T007 |
| Verification | — | T008, T009, T010, T011 |
| **Total** | | **11 tasks** |

**MVP scope**: T001 → T002 → T004 → T005 → T006 (endpoint live with dev fallback).  
**Full feature**: all tasks including T007 (CI SHA injection on main builds).
