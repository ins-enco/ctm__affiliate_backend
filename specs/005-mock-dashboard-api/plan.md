# Implementation Plan: Mock Module — Dashboard API

**Branch**: `feature/005-mock-dashboard-api` | **Date**: 2026-04-15 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `specs/005-mock-dashboard-api/spec.md`

## Summary

Deliver five read-only, in-memory GET endpoints under `/api/mock/*` that serve static mock data for the Dashboard screen. No database, no authentication, no pagination — each endpoint returns its full fixed dataset on every call. The module follows the existing modular-monolith pattern (API + Application layers only, no Domain/Infrastructure), consistent with the SubscriptionHistory module.

## Technical Context

**Language/Version**: C# 12 / .NET 8  
**Primary Dependencies**: ASP.NET Core 8, Swashbuckle (Swagger)  
**Storage**: N/A — mocked in-memory static data; no EF, no migrations  
**Testing**: xUnit (unit tests), xUnit + `WebApplicationFactory<Program>` (integration tests)  
**Target Platform**: Linux server (Docker Compose)  
**Project Type**: web-service module (modular monolith plugin)  
**Performance Goals**: All 5 endpoints respond within 500 ms under normal load (SC-001)  
**Constraints**: Fully deterministic — no randomness; no authentication required; no write operations; query parameters silently ignored  
**Scale/Scope**: 5 GET endpoints; mock-only; will be superseded by real-data endpoints in a future iteration

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **Scope boundary** — new modules justify against referral/attribution/affiliate admin | ✅ Justified | Affiliate requests + signal provider requests feed affiliate visibility data. Client requests relate to copy-trade attribution flows. Current-user and user-list power the dashboard that affiliates and operators use. Module explicitly scoped to mock layer only. |
| **P1 — Modules are islands** | ✅ | `Mock.API` references only `Mock.Application`. No inter-module project references. |
| **P5 — Async all the way** | ✅ | Service methods return `Task<T>`; no `.Result`/`.Wait()`. |
| **P6 — Consistent error contract** | ✅ | 405 for non-GET methods is handled automatically by ASP.NET Core routing. No custom error paths required beyond the standard ExceptionHandlingMiddleware. |
| **API convention** `/api/{module}/{resource}` | ✅ | Endpoints: `/api/mock/users`, `/api/mock/current-user`, `/api/mock/client-requests`, `/api/mock/signal-provider-requests`, `/api/mock/affiliate-requests`. |
| **Secrets never in source** | ✅ | No secrets required — mock endpoints are unauthenticated by design. |
| **No auth required** | ✅ per spec | Spec assumption: no authentication or authorization for mock endpoints. |
| **Definition of done gates** | Pending implementation | spec ✅, plan ✅, unit tests pending, integration tests pending, no compiler warnings pending, Swagger docs pending. |

**No constitution violations. Proceed to Phase 0.**

## Project Structure

### Documentation (this feature)

```text
specs/005-mock-dashboard-api/
├── plan.md              # This file (/speckit.plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── mock-users.md
│   ├── mock-current-user.md
│   ├── mock-client-requests.md
│   ├── mock-signal-provider-requests.md
│   └── mock-affiliate-requests.md
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
Backend/
├── src/
│   ├── Host/
│   │   └── CopyTradeMarketApi.Host/
│   │       └── CopyTradeMarketApi.Host.csproj   ← add Mock.API reference
│   └── Modules/
│       └── Mock/                        ← NEW
│           ├── Mock.API/
│           │   ├── Controllers/
│           │   │   └── MockController.cs
│           │   ├── GlobalUsings.cs
│           │   ├── MockModule.cs
│           │   └── Mock.API.csproj
│           └── Mock.Application/
│               ├── DTOs/
│               │   ├── UserDto.cs
│               │   ├── CurrentUserDto.cs
│               │   ├── ClientRequestDto.cs
│               │   ├── SignalProviderRequestDto.cs
│               │   └── AffiliateRequestDto.cs
│               ├── Services/
│               │   ├── IMockService.cs
│               │   └── MockService.cs
│               ├── GlobalUsings.cs
│               └── Mock.Application.csproj
└── tests/
    ├── Mock.Application.Tests/          ← NEW
    │   ├── MockServiceTests.cs
    │   ├── GlobalUsings.cs
    │   └── Mock.Application.Tests.csproj
    └── Integration.Tests/
        └── Mock/                        ← NEW folder in existing project
            └── MockTests.cs
```

**Structure Decision**: Two-layer modular-monolith pattern (API + Application), identical to `SubscriptionHistory`. No Domain or Infrastructure layers — mock-only module requires no entities, EF context, or migrations. A single `MockController` exposes all 5 endpoints; a single `IMockService` / `MockService` owns the static data and returns typed DTOs.

## Implementation Phases

### Phase 1 — Application Layer (DTOs + Service)

1. Create `Mock.Application.csproj` — references `CopyTradeMarketApi.Shared` only
2. Add `GlobalUsings.cs`
3. Create 5 DTO records in `DTOs/`:
   - `UserDto(int Id, string Name, string Role)`
   - `CurrentUserDto(int Id, string Name, string Abbreviation, string Role)`
   - `ClientRequestDto(DateTime Timestamp, string Name, decimal Equity, string Strategy, string StrategyLicense)`
   - `SignalProviderRequestDto(DateTime Timestamp, string Name, string KycStatus)`
   - `AffiliateRequestDto(DateTime Timestamp, string Name, string KycStatus)`
4. Create `IMockService` with 5 methods:
   ```csharp
   Task<List<UserDto>> GetUsersAsync();
   Task<CurrentUserDto> GetCurrentUserAsync();
   Task<List<ClientRequestDto>> GetClientRequestsAsync();
   Task<List<SignalProviderRequestDto>> GetSignalProviderRequestsAsync();
   Task<List<AffiliateRequestDto>> GetAffiliateRequestsAsync();
   ```
5. Implement `MockService` with static in-memory data:
   - `GetUsersAsync`: ≥5 users covering all 3 roles (Client, Signal Provider, Affiliate)
   - `GetCurrentUserAsync`: 1 current user (id, name, 2-char abbreviation, role)
   - `GetClientRequestsAsync`: exactly 10 client request records
   - `GetSignalProviderRequestsAsync`: exactly 10 signal provider request records
   - `GetAffiliateRequestsAsync`: exactly 10 affiliate request records

### Phase 2 — API Layer (Controller + Module)

1. Create `Mock.API.csproj` — references `Mock.Application` + `Microsoft.AspNetCore.App`
2. Add `GlobalUsings.cs`
3. Create `MockController` at route `api/mock`:
   - `GET /api/mock/users` → `GetUsersAsync()`
   - `GET /api/mock/current-user` → `GetCurrentUserAsync()`
   - `GET /api/mock/client-requests` → `GetClientRequestsAsync()`
   - `GET /api/mock/signal-provider-requests` → `GetSignalProviderRequestsAsync()`
   - `GET /api/mock/affiliate-requests` → `GetAffiliateRequestsAsync()`
   - Each returns `Ok(result)` — no query parameters, no filtering
4. Create `MockModule : IModule` — registers `IMockService` as singleton

### Phase 3 — Host Wiring

1. Add `<ProjectReference>` to `Mock.API` in `CopyTradeMarketApi.Host.csproj`
2. Register `MockModule` **conditionally** in `Program.cs` — only when the current environment is `Development`:
   ```csharp
   if (app.Environment.IsDevelopment())
   {
       // register MockModule services and map endpoints
   }
   ```
   In all other environments (Staging, Production, etc.) the module is not registered — calls to `/api/mock/*` return HTTP 404.

### Phase 4 — Unit Tests

Create `Mock.Application.Tests.csproj` (references `Mock.Application`).

Test cases for `MockService`:
- `GetUsersAsync` returns ≥5 users
- `GetUsersAsync` covers all 3 roles (Client, Signal Provider, Affiliate)
- `GetUsersAsync` — every record has non-empty id, name, and role from allowed set
- `GetCurrentUserAsync` returns exactly 1 user
- `GetCurrentUserAsync` — abbreviation is exactly 2 characters
- `GetCurrentUserAsync` — role is from allowed set
- `GetClientRequestsAsync` returns exactly 10 records
- `GetClientRequestsAsync` — every record has positive equity
- `GetClientRequestsAsync` — every record has non-null timestamp, name, strategy, strategyLicense
- `GetSignalProviderRequestsAsync` returns exactly 10 records
- `GetSignalProviderRequestsAsync` — every kycStatus is one of: Pending, Verified, Rejected
- `GetAffiliateRequestsAsync` returns exactly 10 records
- `GetAffiliateRequestsAsync` — every kycStatus is one of: Pending, Verified, Rejected

### Phase 5 — Integration Tests

Add `Mock/MockTests.cs` to existing `Integration.Tests` project.

Test cases (HTTP-level, via `IntegrationWebFactory`):
- `GET /api/mock/users` → 200; body is array with ≥5 items; each has id, name, role
- `GET /api/mock/users` → all roles covered in single response
- `GET /api/mock/current-user` → 200; single object with id, name, 2-char abbreviation, role
- `GET /api/mock/client-requests` → 200; exactly 10 items; each has timestamp, name, equity (> 0), strategy, strategyLicense
- `GET /api/mock/signal-provider-requests` → 200; exactly 10 items; each kycStatus in allowed set
- `GET /api/mock/affiliate-requests` → 200; exactly 10 items; each kycStatus in allowed set
- Swagger JSON contains all 5 mock endpoints under `/api/mock/*`
- When host environment is set to non-Development, `GET /api/mock/users` → 404 (endpoints not registered)

### Phase 6 — Swagger

- XML doc comments on all 5 controller actions (summary + return description)
- Verify Swagger UI exposes all 5 endpoints after wiring

## Complexity Tracking

No constitution violations to justify. No complexity exceptions required.
