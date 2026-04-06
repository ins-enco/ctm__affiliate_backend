# Implementation Plan: API Version Exposure

## Technical Context

### Tech Stack
- .NET 8 / ASP.NET Core — existing host
- Minimal API endpoint (`app.MapGet`) — already used in the project
- `IConfiguration` — already injected; .NET env var override is built-in
- GitHub Actions — new CI workflow (no extra tools or NuGet packages needed)

### Architecture Approach

The version flows through three layers, each overriding the previous:

```
appsettings.json           "ApiVersion": "0.0.0-local"   ← committed fallback, always present
appsettings.Development    "ApiVersion": "0.0.0-dev"      ← local dev override
Environment variable       ApiVersion=<short-sha>         ← CI sets this; wins at runtime
```

.NET's `IConfiguration` reads environment variables automatically and they take precedence over `appsettings.json` values. No file patching, no recompile.

At startup, `Program.cs` reads `builder.Configuration["ApiVersion"]` once and passes the value to both `SwaggerDoc` and the `GET /api/version` minimal API endpoint.

The GitHub Actions workflow triggers on every push to `main`, runs `dotnet test`, and starts the Docker container with `ApiVersion=${{ github.sha }}` as an environment variable.

### Constitution Check
- [x] **P1 — Modules Are Islands**: Host-level plumbing only; no inter-module references.
- [x] **P2 — Specification Pattern**: Not applicable — no database queries.
- [x] **P3 — Domain Events for Side Effects**: Not applicable.
- [x] **P4 — Secrets Never In Source**: `ApiVersion` is not a secret. The git SHA is public build metadata.
- [x] **P5 — Async All the Way**: Endpoint returns a static in-memory value — synchronous is correct.
- [x] **P6 — Consistent Error Contract**: No error paths; endpoint always returns `200 OK`.

---

## Phase 0: Research

### Decisions Made

| Decision | Choice | Rationale | Alternatives |
|---|---|---|---|
| Version value | Short git SHA (`github.sha` first 7 chars) | Unique per commit, zero manual effort, directly traceable | Semver (requires manual bump per PR), date-based (not traceable to code) |
| Injection mechanism | Environment variable `ApiVersion` set by CI | .NET config precedence makes this automatic; no file patching needed | Patching `appsettings.json` in CI (works but mutates tracked file), baking into Docker image (less flexible) |
| Local fallback | `"0.0.0-local"` / `"0.0.0-dev"` in appsettings | FE can trivially distinguish local from production; never `null` | `"unknown"` (less informative), assembly version (requires recompile) |
| CI trigger | `on: push: branches: [main]` | Fires after every merge with the final merge commit SHA | On PR (pre-merge SHA, not what runs in prod), scheduled (not event-driven) |
| Test gate | `dotnet test` before deploy step | Prevents deploying a broken build; consistent with constitution DoD | No gate (risky), separate test job (adds complexity for this size project) |
| Endpoint style | Minimal API (`app.MapGet`) | Already used in `Program.cs`; no controller overhead | Controller action (unnecessary ceremony) |

---

## Phase 1: Design

### Response Contract

`GET /api/version` — no authentication required

```
200 OK
Content-Type: application/json

{
  "version": "a3f9c12"
}
```

Local/dev:
```json
{ "version": "0.0.0-dev" }
```

### Configuration Schema

`appsettings.json`:
```json
{
  "ApiVersion": "0.0.0-local"
}
```

`appsettings.Development.json`:
```json
{
  "ApiVersion": "0.0.0-dev"
}
```

CI environment variable (set by GitHub Actions — never committed):
```
ApiVersion=a3f9c12
```

### GitHub Actions Workflow

`.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test-and-deploy:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: Backend

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Run tests
        run: dotnet test --verbosity normal

      - name: Set short SHA
        if: github.ref == 'refs/heads/main'
        run: echo "SHORT_SHA=$(git rev-parse --short HEAD)" >> $GITHUB_ENV

      - name: Start services with versioned API
        if: github.ref == 'refs/heads/main'
        run: |
          ApiVersion=${{ env.SHORT_SHA }} docker compose up --build -d api
        env:
          DB_ROOT_PASSWORD: ${{ secrets.DB_ROOT_PASSWORD }}
          DB_CONNECTION_STRING: ${{ secrets.DB_CONNECTION_STRING }}
          JWT_SECRET_KEY: ${{ secrets.JWT_SECRET_KEY }}
          LOKI_PASSWORD: ${{ secrets.LOKI_PASSWORD }}
          ApiVersion: ${{ env.SHORT_SHA }}
```

`docker-compose.yml` — add `ApiVersion` to the `api` service environment:
```yaml
api:
  environment:
    ApiVersion: "${ApiVersion:-0.0.0-local}"
```

### Project Structure

```
Backend/
└── src/Host/CopyTradeMarketApi.Host/
    ├── appsettings.json                  # ADD "ApiVersion": "0.0.0-local"
    ├── appsettings.Development.json      # ADD "ApiVersion": "0.0.0-dev"
    ├── docker-compose.yml                # ADD ApiVersion env var to api service
    └── Program.cs
        ├── SwaggerDoc "v1"               # CHANGE Version to read from config
        └── app.MapGet("/api/version")    # ADD minimal API endpoint

.github/
└── workflows/
    └── ci.yml                            # NEW — test gate + inject SHORT_SHA
```

---

## Phase 2: Implementation Steps

### Step 1 — Add `ApiVersion` to configuration files

`appsettings.json` → add `"ApiVersion": "0.0.0-local"`  
`appsettings.Development.json` → add `"ApiVersion": "0.0.0-dev"`

### Step 2 — Expose `ApiVersion` through Docker Compose

`docker-compose.yml` → add to `api.environment`:
```yaml
ApiVersion: "${ApiVersion:-0.0.0-local}"
```
The `:-` default means local `docker compose up` without the env var still works.

### Step 3 — Wire version into `Program.cs`

Read config before `AddSwaggerGen`:
```csharp
var apiVersion = builder.Configuration["ApiVersion"] ?? "0.0.0-local";
```

Update `SwaggerDoc`:
```csharp
options.SwaggerDoc("v1", new() { Title = "CopyTrade Market API", Version = apiVersion });
```

Add endpoint after `app.MapControllers()`:
```csharp
app.MapGet("/api/version", () => Results.Ok(new { version = apiVersion }))
   .WithTags("Meta")
   .AllowAnonymous();
```

### Step 4 — Create GitHub Actions workflow

Create `.github/workflows/ci.yml` (content in Phase 1 Design above).

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `ApiVersion` env var not passed to container → returns fallback | Low | Low | `:-0.0.0-local` default in `docker-compose.yml`; FE treats any non-SHA as dev | 
| GitHub Actions secrets not configured → deploy step fails | Medium | Medium | Step is gated on `github.ref == 'refs/heads/main'`; test step still runs on PRs |
| FE caches old version response | Low | Low | `Cache-Control: no-store` header can be added later if needed |
| SHA collisions between repos/builds | Negligible | Low | SHA is unique per commit on the same repo |
app.MapGet("/api/version", () => Results.Ok(new { version = apiVersion }))
   .WithTags("Meta")
   .AllowAnonymous();
```

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Developer forgets to bump `ApiVersion` when making a breaking change | Medium | Low | Convention is documented in spec; enforced by code review, not tooling |
| `ApiVersion` key missing from config in some environment | Low | Low | `?? "1.0.0"` fallback in `Program.cs` ensures the app starts regardless |
| FE polls `/api/version` excessively and adds load | Low | Low | Response is static in-memory — trivial overhead; can add `Cache-Control` header later if needed |
