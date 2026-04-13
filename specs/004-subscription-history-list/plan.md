# Implementation Plan: Subscription History List Endpoint

**Branch**: `004-subscription-history-list` | **Date**: 2026-04-13 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/004-subscription-history-list/spec.md`

## Summary

Expose a `GET /api/subscription-history` endpoint that returns a list of subscription history records (client subscribe/unsubscribe events for trading strategies). Data is served from an in-memory mocked dataset — no database tables or migrations required.

The endpoint supports three orthogonal optional capabilities applied in this order: **filter → sort → paginate**:
- **Filtering**: `query` (partial match on client name, account number, strategy name)
- **Ordering**: `orderBy` (field name, default `timestamp`) + `orderDirection` (`asc` / `desc`, default `desc`)
- **Pagination**: `page` + `pageSize` (omitting both returns all matching records)

A new `SubscriptionHistory` module (2 layers: API + Application) is added to the modular monolith.

## Technical Context

**Language/Version**: C# 12 / .NET 8  
**Primary Dependencies**: ASP.NET Core 8, Swashbuckle (Swagger), xUnit + Moq  
**Storage**: N/A — mocked in-memory list (no EF, no migrations)  
**Testing**: xUnit + Moq (unit tests), `WebApplicationFactory<Program>` + SQLite (integration tests)  
**Target Platform**: Linux server via Docker Compose  
**Project Type**: web-service (modular monolith)  
**Performance Goals**: ≤500ms p95 response time (SC-001)  
**Constraints**: Async all the way (P5); ProblemDetails for all errors (P6); no secrets in source (P4); no `[Authorize]` this iteration  
**Scale/Scope**: Single endpoint; 20 mocked records; no DB dependency

## Constitution Check

| Gate | Status | Notes |
|------|--------|-------|
| Module isolation (P1) | PASS | New `SubscriptionHistory` module — no inter-module project references |
| Single Responsibility (S) | PASS | Controller routes; Service holds mock data + pagination logic; DTOs are pure records |
| Async all the way (P5) | PASS | Service method returns `Task<PagedResponse<SubscriptionHistoryItem>>`; no `.Result`/`.Wait()` |
| ProblemDetails errors (P6) | PASS | Invalid pagination → `ExceptionHandlingMiddleware` surfaces RFC 7807 ProblemDetails |
| No secrets in source (P4) | PASS | No DB connection strings; no secrets involved |
| EF migration rule | PASS | Mocked data — no migration needed this iteration |
| API route convention | PASS | `GET /api/subscription-history` follows `/api/{module}/{resource}` pattern |
| Swagger docs | PASS | Controller decorated with XML doc comments; Swagger auto-generates spec |
| Scope boundary | NOTE | Subscription history is downstream lifecycle data for affiliate-attributed users. Acceptable for this iteration; revisit if expanded beyond affiliate context (see research.md Decision 1). |

_Re-checked after Phase 1 design: no new violations introduced._

## Project Structure

### Documentation (this feature)

```text
specs/004-subscription-history-list/
├── plan.md              ← this file
├── research.md          ← Phase 0 decisions
├── data-model.md        ← Phase 1 data shapes
├── contracts/
│   └── subscription-history.md   ← HTTP API contract
└── tasks.md             ← Phase 2 output (/speckit.tasks — not yet created)
```

### Source Code

```text
Backend/src/
├── Host/
│   └── CopyTradeMarketApi.Host/
│       └── Program.cs              ← register IModule for SubscriptionHistory
└── Modules/
    └── SubscriptionHistory/
        ├── SubscriptionHistory.API/
        │   ├── Controllers/
        │   │   └── SubscriptionHistoryController.cs
        │   ├── SubscriptionHistoryModule.cs      ← IModule implementation
        │   └── SubscriptionHistory.API.csproj
        └── SubscriptionHistory.Application/
            ├── Services/
            │   ├── ISubscriptionHistoryService.cs
            │   └── SubscriptionHistoryService.cs
            ├── DTOs/
            │   └── SubscriptionHistoryItem.cs
            ├── GlobalUsings.cs
            └── SubscriptionHistory.Application.csproj
            (response envelope = PagedResponse<SubscriptionHistoryItem> from CopyTradeMarketApi.Shared.Responses)

Backend/tests/
├── SubscriptionHistory.Application.Tests/
│   └── SubscriptionHistoryServiceTests.cs
└── Integration.Tests/
    └── SubscriptionHistory/
        └── SubscriptionHistoryTests.cs
```

**Structure Decision**: 2-layer module (API + Application only). No Domain or Infrastructure layers because there are no domain entities with business rules and no database persistence (mocked data). Full 4-layer structure is added when real persistence is introduced. New `SubscriptionHistory.Application.Tests` project for unit tests; integration tests added to the existing `Integration.Tests` project.

## Implementation Steps

### Step 1 — SubscriptionHistory.Application project

Create `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.Application/SubscriptionHistory.Application.csproj`:
- Target: `net8.0`
- Nullable: enable
- Project reference: `CopyTradeMarketApi.Shared` (required for `PagedResponse<T>`)

Files to create:
1. `DTOs/SubscriptionHistoryItem.cs` — record with 7 fields (see data-model.md)
2. `Services/ISubscriptionHistoryService.cs` — single method `Task<PagedResponse<SubscriptionHistoryItem>> GetAsync(int? page, int? pageSize, string? query, string? orderBy, string? orderDirection)` (uses `PagedResponse<T>` from `CopyTradeMarketApi.Shared.Responses` — no custom response DTO needed)
3. `Services/SubscriptionHistoryService.cs` — singleton service holding static mocked list; applies filter → sort → paginate and returns `PagedResponse<SubscriptionHistoryItem>.All()` or `.Paginated()` depending on params
4. `GlobalUsings.cs` — includes `global using CopyTradeMarketApi.Shared.Responses;`

> **Note**: No `SubscriptionHistoryResponse.cs` is created — spec 003 provides `PagedResponse<T>` in `CopyTradeMarketApi.Shared` for this purpose.

### Step 2 — SubscriptionHistory.API project

Create `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.API/SubscriptionHistory.API.csproj`:
- Target: `net8.0`
- Nullable: enable
- Project reference: `SubscriptionHistory.Application`

Files to create:
1. `Controllers/SubscriptionHistoryController.cs` — `GET /api/subscription-history` with optional `[FromQuery] int? page, int? pageSize, string? query, string? orderBy, string? orderDirection`
2. `SubscriptionHistoryModule.cs` — implements `IModule`; registers `ISubscriptionHistoryService` as singleton and maps controller routes
3. `GlobalUsings.cs`

### Step 3 — Host wiring

In `Backend/src/Host/CopyTradeMarketApi.Host/Program.cs`:
- Add project reference to `SubscriptionHistory.API`
- Register `SubscriptionHistoryModule` alongside existing modules

### Step 4 — Unit tests

Create `Backend/tests/SubscriptionHistory.Application.Tests/SubscriptionHistory.Application.Tests.csproj`:
- Target: `net8.0`
- References: `SubscriptionHistory.Application`, xUnit, Moq

Test cases in `SubscriptionHistoryServiceTests.cs`:
- `GetAsync_WithNoPagination_ReturnsAllRecords` — verify all mocked records returned; pagination fields null
- `GetAsync_WithPageAndPageSize_ReturnsCorrectSlice` — page=1, pageSize=5 → first 5 records
- `GetAsync_WithPageBeyondLast_ReturnsEmptyItems` — large page number → empty items, correct totalCount
- `GetAsync_WithPageSizeOnly_DefaultsPageToOne` — pageSize=5 without page → same as page=1
- `GetAsync_WithPageOnly_DefaultsPageSizeToTwenty` — page=1 without pageSize → pageSize=20 in response
- `GetAsync_WithZeroPage_ThrowsArgumentException` — invalid input guard
- `GetAsync_WithQuery_FiltersByClientAccountOrStrategy` — query filter is case-insensitive and partial
- `GetAsync_WithOrderByClientName_DefaultsToDescending` — sorting by field works
- `GetAsync_WithOrderDirectionOnly_AppliesToDefaultTimestamp` — direction-only request applies to default field
- `GetAsync_WithInvalidOrderBy_ThrowsArgumentException` — invalid orderBy rejected
- `GetAsync_WithInvalidOrderDirection_ThrowsArgumentException` — invalid orderDirection rejected

### Step 5 — Integration tests

Add to existing `Backend/tests/Integration.Tests/SubscriptionHistory/SubscriptionHistoryTests.cs`:
- `GetSubscriptionHistory_NoPagination_Returns200WithAllRecords`
- `GetSubscriptionHistory_WithValidPagination_Returns200WithMetadata`
- `GetSubscriptionHistory_WithZeroPage_Returns400ProblemDetails`
- `GetSubscriptionHistory_WithZeroPageSize_Returns400ProblemDetails`
- `GetSubscriptionHistory_WithPageBeyondTotal_Returns200EmptyItems`
- `GetSubscriptionHistory_WithQueryFilter_ReturnsMatchingRows`
- `GetSubscriptionHistory_WithOrderByAndDirection_ReturnsSortedRows`
- `GetSubscriptionHistory_WithInvalidOrderBy_Returns400ProblemDetails`
- `GetSubscriptionHistory_WithInvalidOrderDirection_Returns400ProblemDetails`

### Step 6 — Swagger / solution file

- Ensure `SubscriptionHistory.API.csproj` and `SubscriptionHistory.Application.csproj` are added to `Backend/CopyTradeMarketApi.slnx`
- Verify `GET /api/subscription-history` appears in Swagger UI after `dotnet run`
- Verify Swagger lists all query parameters: `query`, `orderBy`, `orderDirection`, `page`, `pageSize`

## Complexity Tracking

No constitution violations — no justification table needed.
