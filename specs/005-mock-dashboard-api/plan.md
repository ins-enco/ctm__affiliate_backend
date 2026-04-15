# Implementation Plan: Mock Module — Dashboard API

**Branch**: `feature/005-mock-dashboard-api` | **Date**: 2026-04-15 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `specs/005-mock-dashboard-api/spec.md`

## Summary

Deliver five read-only, in-memory GET endpoints under `/api/dashboard/*` (plus `GET /api/currentActiveUser`) that serve static mock data for the Dashboard screen. No database, no authentication except the `API-KEY` header check on `currentActiveUser` in Development. `listOfUsers` supports optional `searchText` name filtering. The module follows the existing modular-monolith pattern (API + Application layers only, no Domain/Infrastructure), consistent with the SubscriptionHistory module.

## Technical Context

**Language/Version**: C# 12 / .NET 8  
**Primary Dependencies**: ASP.NET Core 8, Swashbuckle (Swagger)  
**Storage**: N/A — mocked in-memory static data; no EF, no migrations  
**Testing**: xUnit (unit tests), xUnit + `WebApplicationFactory<Program>` (integration tests)  
**Target Platform**: Linux server (Docker Compose)  
**Project Type**: web-service module (modular monolith plugin)  
**Performance Goals**: All 5 endpoints respond within 500 ms under normal load (SC-001)  
**Constraints**: Fully deterministic — no randomness; no write operations; `searchText` on `listOfUsers` filters by name (case-insensitive contains); `GET /api/currentActiveUser` (no path param) is protected by `API-KEY` header validation in Development (returns 401 if absent or wrong value); `DevApiKeyFilter` is registered in DI and applied to the `currentActiveUser` action only; the filter short-circuits in non-Development environments  
**Scale/Scope**: 5 GET endpoints; mock-only; will be superseded by real-data endpoints in a future iteration

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **Scope boundary** — new modules justify against referral/attribution/affiliate admin | ✅ Justified | Affiliate requests + signal provider requests feed affiliate visibility data. Client requests relate to copy-trade attribution flows. Current-user and user-list power the dashboard that affiliates and operators use. Module explicitly scoped to mock layer only. |
| **P1 — Modules are islands** | ✅ | `Mock.API` references only `Mock.Application`. No inter-module project references. |
| **P5 — Async all the way** | ✅ | Service methods return `Task<T>`; no `.Result`/`.Wait()`. |
| **P6 — Consistent error contract** | ✅ | 405 for non-GET methods is handled automatically by ASP.NET Core routing. No custom error paths required beyond the standard ExceptionHandlingMiddleware. |
| **API convention** `/api/{module}/{resource}` | ✅ | Endpoints: `GET /api/dashboard/listOfUsers`, `GET /api/currentActiveUser`, `GET /api/dashboard/clientRequests`, `GET /api/dashboard/signalProviderRequests`, `GET /api/dashboard/affiliateRequests`. Route split: `[Route("api/dashboard")]` covers 4 endpoints; `[Route("api")]` + `[HttpGet("currentActiveUser")]` covers the active-user endpoint. |
| **Secrets never in source** | ✅ | `SimulatedKeyForDev` is a hard-coded placeholder value with no real security value. It is only used in the Development environment for mock endpoints. |
| **No auth required** | ✅ per spec (except currentActiveUser) | `GET /api/currentActiveUser` requires `API-KEY: SimulatedKeyForDev` header in Development (enforced by `DevApiKeyFilter`). All other mock endpoints are unauthenticated. |
| **Definition of done gates** | ✅ Complete | spec ✅, plan ✅, unit tests ✅, integration tests ✅, no compiler warnings ✅, Swagger docs ✅. |

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
│   ├── Shared/
│   │   └── CopyTradeMarketApi.Shared/
│   │       └── Filters/
│   │           └── DevApiKeyFilter.cs   ← reusable across all modules
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
   - `UserDto(string Id, string Name, string Role)`
   - `CurrentUserDto(string Id, string Name, string Abbreviation, string Role)`
   - `ClientRequestDto(DateTime Timestamp, string Name, decimal Equity, string Strategy, string StrategyLicense)`
   - `SignalProviderRequestDto(DateTime Timestamp, string Name, string KycStatus)`
   - `AffiliateRequestDto(DateTime Timestamp, string Name, string KycStatus)`
4. Create `IMockService` with 5 methods:
   ```csharp
   Task<PagedResponse<UserDto>> GetUsersAsync(string? searchText = null);
   Task<CurrentUserDto> GetCurrentUserAsync();
   Task<PagedResponse<ClientRequestDto>> GetClientRequestsAsync();
   Task<PagedResponse<SignalProviderRequestDto>> GetSignalProviderRequestsAsync();
   Task<PagedResponse<AffiliateRequestDto>> GetAffiliateRequestsAsync();
   ```
5. Implement `MockService` with static in-memory data:
   - `GetUsersAsync`: returns `PagedResponse<UserDto>.All(filteredUsers)` (apply `searchText` filter when non-empty)
   - `GetCurrentUserAsync`: returns the static `CurrentUserDto` (plain object — not wrapped)
   - `GetClientRequestsAsync`: returns `PagedResponse<ClientRequestDto>.All(_clientRequests)` — exactly 10 records
   - `GetSignalProviderRequestsAsync`: returns `PagedResponse<SignalProviderRequestDto>.All(_signalProviderRequests)` — exactly 10 records
   - `GetAffiliateRequestsAsync`: returns `PagedResponse<AffiliateRequestDto>.All(_affiliateRequests)` — exactly 10 records

### Phase 2 — API Layer (Controller + Module)

1. Create `Mock.API.csproj` — references `Mock.Application` + `Microsoft.AspNetCore.App`
2. Add `GlobalUsings.cs`
3. Create `MockController` with route base `[Route("api")]`:
   - `[HttpGet("dashboard/listOfUsers")]` → `GetUsersAsync([FromQuery] string? searchText = null)` — passes `searchText` to service; returns filtered or full user list
   - `[HttpGet("currentActiveUser")]` + `[ServiceFilter(typeof(DevApiKeyFilter))]` → `GetCurrentUserAsync()` — returns the static current user; requires `API-KEY: SimulatedKeyForDev` header in Development (401 otherwise)
   - `[HttpGet("dashboard/clientRequests")]` → `GetClientRequestsAsync()`
   - `[HttpGet("dashboard/signalProviderRequests")]` → `GetSignalProviderRequestsAsync()`
   - `[HttpGet("dashboard/affiliateRequests")]` → `GetAffiliateRequestsAsync()`
   - Each returns `Ok(result)` — no extra filtering beyond what the service provides
4. Create `MockModule : IModule` — registers `IMockService` as singleton and `DevApiKeyFilter` as scoped (required for `ServiceFilter` resolution)

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
- `GetCurrentUserAsync` returns exactly 1 user (no arguments)
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
- `GET /api/dashboard/listOfUsers` → 200; body is array with ≥5 items; each has id, name, role
- `GET /api/dashboard/listOfUsers` → all roles covered in single response
- `GET /api/dashboard/listOfUsers?searchText=abc` → 200; only users with "abc" in name returned
- `GET /api/currentActiveUser` with `API-KEY: SimulatedKeyForDev` header → 200; single object with id (string), name, 2-char abbreviation, role
- `GET /api/currentActiveUser` without `API-KEY` header → 401 Unauthorized
- `GET /api/currentActiveUser` with wrong `API-KEY` value → 401 Unauthorized
- `GET /api/dashboard/clientRequests` → 200; exactly 10 items; each has timestamp, name, equity (> 0), strategy, strategyLicense
- `GET /api/dashboard/signalProviderRequests` → 200; exactly 10 items; each kycStatus in allowed set
- `GET /api/dashboard/affiliateRequests` → 200; exactly 10 items; each kycStatus in allowed set
- Swagger JSON contains all 5 mock endpoints under the new route paths
- When host environment is set to non-Development, `GET /api/dashboard/listOfUsers` → 404 (endpoints not registered)

### Phase 6 — Swagger

- XML doc comments on all 5 controller actions (summary + return description)
- Verify Swagger UI exposes all 5 endpoints after wiring

## Complexity Tracking

No constitution violations to justify. No complexity exceptions required.
