# Feature: API Version Exposure

---
id: api-version-exposure
version: 2.0.0
status: draft
owners:
  - tech-lead
ratified:
last-reviewed: 2026-04-03
---

## Overview

The frontend needs a reliable, machine-readable way to detect when the backend API has changed — so it can prompt users to refresh, conditionally enable new features, or detect breaking contract changes during development.

Currently the API has a hardcoded `version: "v1"` in its Swagger document that never changes. This feature introduces an `ApiVersion` configuration key that:
- holds a static fallback locally (`"0.0.0-local"`)
- is **automatically overwritten by the CI pipeline** with the short git commit SHA of the `main` branch build after every merged PR

This means every deployment has a unique, traceable version with zero manual effort.

---

## Constraints

- No new NuGet dependencies
- No URL-based route versioning (`/v1/`, `/v2/`) — that is a separate, larger concern
- Version must be injectable via environment variable without a code change
- The version endpoint must be publicly accessible (no auth required)
- No manual version bumping — version is set by CI, not developers

---

## User Stories

### US1 — Machine-readable version endpoint (P1)

**As a** frontend developer  
**I want** a lightweight, unauthenticated endpoint that returns the current API version  
**So that** the FE can detect API updates on startup or on demand

**Acceptance Criteria:**
- [ ] `GET /api/version` returns `200 OK` with body `{ "version": "<current>" }` and no auth header required
- [ ] The endpoint is visible in Swagger UI under a `Meta` tag
- [ ] The response is consistent with the version shown in `GET /swagger/v1/swagger.json` (`info.version`)
- [ ] When running locally, returns `"0.0.0-local"` or `"0.0.0-dev"` depending on environment
- [ ] After a CI deployment, returns the short git SHA of the merge commit (e.g. `"a3f9c12"`)

---

### US2 — Version in OpenAPI `info.version` (P1)

**As a** frontend developer  
**I want** the Swagger document's `info.version` field to reflect the deployed version  
**So that** Swagger UI and tooling show which exact build is running

**Acceptance Criteria:**
- [ ] `GET /swagger/v1/swagger.json` returns `info.version` matching the `ApiVersion` config value
- [ ] Both `GET /api/version` and `info.version` always return the same value

---

### US3 — Configuration-driven, environment-variable overridable (P1)

**As a** developer  
**I want** the version to fall back to a safe placeholder locally and be overridden by CI at deploy time  
**So that** no manual code change is ever needed to propagate the version

**Acceptance Criteria:**
- [ ] `appsettings.json` contains `"ApiVersion": "0.0.0-local"` as a committed fallback
- [ ] `appsettings.Development.json` contains `"ApiVersion": "0.0.0-dev"` to distinguish local dev runs
- [ ] The `ApiVersion` environment variable overrides `appsettings.json` at runtime (standard .NET config precedence)
- [ ] No version string is hardcoded in C# source files

---

### US4 — CI pipeline injects git SHA on every main build (P1)

**As a** developer  
**I want** every merge to `main` to automatically produce a unique API version equal to the short git SHA  
**So that** the FE always sees a different version after each deployment with no manual action

**Acceptance Criteria:**
- [ ] A GitHub Actions workflow triggers on `push` to `main`
- [ ] The workflow runs `dotnet test` — deployment only proceeds if all tests pass
- [ ] The workflow sets environment variable `ApiVersion` to `$(git rev-parse --short HEAD)` before starting the container
- [ ] The running container's `GET /api/version` returns the short SHA, not the fallback value
- [ ] The workflow is committed at `.github/workflows/ci.yml`

---

## How CI Injects the Version

.NET's configuration system reads environment variables at startup and they take precedence over `appsettings.json`. The CI pipeline sets `ApiVersion` as a Docker environment variable — no file patching, no recompile needed.

```
appsettings.json:          "ApiVersion": "0.0.0-local"   ← committed fallback
appsettings.Development:   "ApiVersion": "0.0.0-dev"     ← local dev override
Environment variable:      ApiVersion=a3f9c12            ← CI sets this, wins at runtime
```

GitHub Actions workflow (`.github/workflows/ci.yml`) on push to `main`:
1. Checkout code
2. Run `dotnet test` — fail fast if tests fail
3. Build Docker image
4. Start container with `ApiVersion=${{ github.sha }}` (short) as env var
5. FE reads `GET /api/version` → `{ "version": "a3f9c12" }`

---

## Out of Scope

- URL-based API versioning (`/v1/`, `/v2/` routing)
- Semantic versioning (MAJOR.MINOR.PATCH) — not required; git SHA is sufficient for FE detection
- Automated changelog or release notes generation
- Deprecation policies or `Sunset` response headers
- FE implementation details (polling strategy, toast notifications, etc.)

---

## Success Metrics

- `GET /api/version` returns the git SHA of the running build after every CI deployment
- `GET /swagger/v1/swagger.json` `info.version` matches `GET /api/version`
- Zero manual steps required to propagate the version after a merge to `main`

---

## Open Questions

- Should `GET /api/version` include additional metadata (e.g. `buildDate`, `environment`)? Defer unless FE requests it.

---

## Out of Scope

- URL-based API versioning (`/v1/`, `/v2/` routing)
- Automated changelog or release notes generation
- Deprecation policies or `Sunset` response headers
- FE implementation details (polling strategy, toast notifications, etc.)

---

## Success Metrics

- `GET /api/version` returns the correct version string in all environments
- `GET /swagger/v1/swagger.json` `info.version` matches `appsettings.json`
- Version is surfaced in Swagger UI without manual code edits after a bump

---

## Open Questions

- Should `GET /api/version` include additional metadata (e.g. `buildDate`, `environment`)? Defer unless FE requests it.
