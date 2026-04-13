# Implementation Plan: Generic Paginated Response

**Branch**: `feature/003-generic-paged-response` | **Date**: 2026-04-13 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/003-generic-paged-response/spec.md`

## Summary

Add `PagedResponse<T>` — a generic, immutable record with two static factory methods (`All` and `Paginated`) — to `CopyTradeMarketApi.Shared.Responses`. This becomes the single shared envelope for any endpoint that returns a list, whether paginated or not. A new `CopyTradeMarketApi.Shared.Tests` project provides unit tests. No database changes, no new HTTP endpoints, no new project references for existing modules.

## Technical Context

**Language/Version**: C# 12 / .NET 8  
**Primary Dependencies**: None new — `System.Collections.Generic` (already in scope), xUnit (for tests)  
**Storage**: N/A — library type only, no persistence  
**Testing**: xUnit (unit tests — no Moq needed; no mocking required for a record type)  
**Target Platform**: N/A — shared library compiled into all modules  
**Project Type**: Library (shared component within modular monolith)  
**Performance Goals**: N/A — object instantiation; no I/O  
**Constraints**: Must not introduce new project dependencies; must be generic over unconstrained `T`; immutable; JSON-serializable via host's global camelCase policy  
**Scale/Scope**: 1 file, 1 new test project, 1 `.slnx` update

## Constitution Check

| Gate | Status | Notes |
|------|--------|-------|
| Single Responsibility (S) | PASS | `PagedResponse<T>` is a pure data container; no business logic |
| Open/Closed (O) | PASS | New type added to Shared; nothing existing is modified |
| Module isolation (P1) | PASS | Added to Shared (not a module); no inter-module project references |
| Records for DTOs | PASS | `record PagedResponse<T>` is idiomatic per constitution |
| Async all the way (P5) | N/A | No I/O operations |
| No secrets in source (P4) | PASS | No configuration or secrets involved |
| No new project dependencies | PASS | Shared already referenced by all modules; Shared.Tests references only Shared + xUnit |
| Swagger / API docs | N/A | No new HTTP endpoints in this feature |
| EF migration rule | N/A | No database entities |
| Scope boundary | PASS | Cross-cutting shared utility — correct placement in Shared |

_Re-checked after Phase 1 design: no violations introduced._

## Project Structure

### Documentation (this feature)

```text
specs/003-generic-paged-response/
├── plan.md              ← this file
├── research.md          ← Phase 0 decisions (7 decisions)
├── data-model.md        ← Phase 1: type design + JSON shapes + adoption example
├── contracts/
│   └── paged-response-type.md   ← C# type contract + caller contract + JSON guarantee
└── tasks.md             ← Phase 2 output (/speckit.tasks — not yet created)
```

### Source Code

```text
Backend/src/Shared/CopyTradeMarketApi.Shared/
└── Responses/
    └── PagedResponse.cs         ← NEW: generic record + 2 factory methods

Backend/tests/
└── CopyTradeMarketApi.Shared.Tests/      ← NEW project
    ├── CopyTradeMarketApi.Shared.Tests.csproj
    ├── Responses/
    │   └── PagedResponseTests.cs
    └── GlobalUsings.cs

Backend/CopyTradeMarketApi.slnx           ← ADD: Shared.Tests entry under /tests/
```

**Structure Decision**: Single file added to existing Shared project under a new `Responses/` subfolder (consistent with `Abstractions/`, `Exceptions/`, `Validation/` pattern). New `Shared.Tests` project isolates Shared unit tests from module-specific test projects.

## Implementation Steps

### Step 1 — Add PagedResponse\<T\> to Shared

Create `Backend/src/Shared/CopyTradeMarketApi.Shared/Responses/PagedResponse.cs`:
- Namespace: `CopyTradeMarketApi.Shared.Responses`
- Positional record with 5 fields: `Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages`
- Static factory `All(IReadOnlyList<T> items)` — sets `TotalCount = items.Count`, nulls pagination fields
- Static factory `Paginated(items, totalCount, page, pageSize)` — computes `TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)`

### Step 2 — Create Shared.Tests project

Create `Backend/tests/CopyTradeMarketApi.Shared.Tests/CopyTradeMarketApi.Shared.Tests.csproj`:
- Target: `net8.0`
- References: `CopyTradeMarketApi.Shared`, xUnit, xUnit.runner.visualstudio, coverlet.collector

### Step 3 — Write unit tests

Create `Backend/tests/CopyTradeMarketApi.Shared.Tests/Responses/PagedResponseTests.cs`:

| Test method | Scenario | Assertion |
|-------------|----------|-----------|
| `All_WithItems_ReturnsTotalCountEqualToItemsCount` | `All(list)` | `TotalCount == list.Count` |
| `All_WithItems_ReturnsNullPaginationFields` | `All(list)` | `Page == null`, `PageSize == null`, `TotalPages == null` |
| `All_WithEmptyList_ReturnsZeroTotalCount` | `All([])` | `TotalCount == 0`, `Items.Count == 0` |
| `Paginated_WithFullPage_ReturnsCorrectMetadata` | `Paginated(5 items, 20, 1, 5)` | `Page==1`, `PageSize==5`, `TotalPages==4`, `TotalCount==20` |
| `Paginated_ComputesTotalPagesWithCeiling` | `Paginated(5 items, 21, 1, 5)` | `TotalPages==5` (ceiling of 21/5) |
| `Paginated_WithTotalCountZero_ReturnsTotalPagesZero` | `Paginated([], 0, 1, 10)` | `TotalPages==0` |
| `Paginated_WithEmptyPage_ReturnsEmptyItems` | `Paginated([], 20, 3, 10)` | `Items.Count==0`, `TotalCount==20` |

### Step 4 — Add GlobalUsings.cs

Create `Backend/tests/CopyTradeMarketApi.Shared.Tests/GlobalUsings.cs`:
```csharp
global using Xunit;
global using CopyTradeMarketApi.Shared.Responses;
```

### Step 5 — Register in solution

Add to `Backend/CopyTradeMarketApi.slnx` under the `/tests/` folder:
```xml
<Project Path="tests/CopyTradeMarketApi.Shared.Tests/CopyTradeMarketApi.Shared.Tests.csproj" />
```

### Step 6 — Update spec 004 notes

Note in `specs/004-subscription-history-list/data-model.md` that `SubscriptionHistoryResponse` is replaced by `PagedResponse<SubscriptionHistoryItem>` once spec 003 is implemented. Actual code change is deferred to spec 004 implementation.

## Complexity Tracking

No constitution violations — no justification table needed.
