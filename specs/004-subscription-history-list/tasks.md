# Tasks: Subscription History List Endpoint

**Input**: Design documents from `/specs/004-subscription-history-list/`  
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/ ✅

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the two new projects and wire them into the solution.

- [X] T001 Create `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.Application/SubscriptionHistory.Application.csproj` targeting net8.0 with nullable enabled; add a project reference to `CopyTradeMarketApi.Shared` (required for `PagedResponse<T>`)
- [X] T002 Create `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.API/SubscriptionHistory.API.csproj` targeting net8.0, referencing `SubscriptionHistory.Application` only (Shared is already transitively available via Application)
- [X] T003 [P] Create `Backend/tests/SubscriptionHistory.Application.Tests/SubscriptionHistory.Application.Tests.csproj` targeting net8.0, referencing SubscriptionHistory.Application, xUnit, and Moq
- [X] T004 Add all three new projects to `Backend/CopyTradeMarketApi.slnx` under the appropriate folder elements so `dotnet build` picks them up

**Checkpoint**: `dotnet build` succeeds with all new projects included (no code yet — just empty project files).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared DTOs, service interface, and module wiring that both user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T005 [P] Create `SubscriptionHistoryItem` record in `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.Application/DTOs/SubscriptionHistoryItem.cs` with fields: `DateTime Timestamp`, `string ClientName`, `string AccountNumber`, `string StrategyName`, `decimal EquityConnect`, `decimal? EquityDisconnect`, `string ActionType`
- [X] T006 [P] Create `GlobalUsings.cs` in `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.Application/GlobalUsings.cs` including `global using CopyTradeMarketApi.Shared.Responses;` so `PagedResponse<T>` is in scope across the project (no custom `SubscriptionHistoryResponse.cs` needed — spec 003 provides this via `PagedResponse<SubscriptionHistoryItem>`)
- [X] T007 [P] Create `GlobalUsings.cs` in `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.API/GlobalUsings.cs` with API-level usings
- [X] T008 Create `ISubscriptionHistoryService` interface in `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.Application/Services/ISubscriptionHistoryService.cs` with single method: `Task<PagedResponse<SubscriptionHistoryItem>> GetAsync(int? page, int? pageSize)`
- [X] T009 Create `SubscriptionHistoryModule.cs` in `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.API/SubscriptionHistoryModule.cs` implementing `IModule`; register `ISubscriptionHistoryService` as singleton and map controller routes
- [X] T010 Add project reference to `SubscriptionHistory.API` in `Backend/src/Host/CopyTradeMarketApi.Host/CopyTradeMarketApi.Host.csproj` and register `SubscriptionHistoryModule` in `Program.cs` alongside existing modules

**Checkpoint**: `dotnet build` succeeds. All interfaces and DTOs compile. Module is registered but service implementation and controller do not yet exist.

---

## Phase 3: User Story 1 — Retrieve All Subscription History (Priority: P1) 🎯 MVP

**Goal**: `GET /api/subscription-history` with no query parameters returns all 20 mocked records in a consistent response envelope with `page`/`pageSize`/`totalPages` as `null`.

**Independent Test**: Call `GET /api/subscription-history` with no params → verify 200 OK, `items` array length equals 20, `totalCount` equals 20, `page` is `null`, `pageSize` is `null`, `totalPages` is `null`, records are newest-first.

### Tests for User Story 1

- [X] T011 [US1] Add test method `GetAsync_WithNoPagination_ReturnsAllRecords` to `Backend/tests/SubscriptionHistory.Application.Tests/SubscriptionHistoryServiceTests.cs`: construct `SubscriptionHistoryService` directly; call `GetAsync(null, null)`; assert `Items.Count == 20`, `TotalCount == 20`, `Page == null`, `PageSize == null`, `TotalPages == null`
- [X] T012 [US1] Add integration test `GetSubscriptionHistory_NoPagination_Returns200WithAllRecords` to `Backend/tests/Integration.Tests/SubscriptionHistory/SubscriptionHistoryTests.cs`: GET `/api/subscription-history`; assert 200, deserialize response, verify `items.Count == 20`, `totalCount == 20`, `page == null`

### Implementation for User Story 1

- [X] T013 [US1] Implement `SubscriptionHistoryService` in `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.Application/Services/SubscriptionHistoryService.cs`: initialize a private `static readonly IReadOnlyList<SubscriptionHistoryItem>` with 20 mocked records sorted newest-first (mix of Subscribe/Unsubscribe, at least 3 distinct client names and strategies, `EquityDisconnect` null for Subscribe rows); `GetAsync(null, null)` returns `PagedResponse<SubscriptionHistoryItem>.All(mockedList)` — page/pageSize/totalPages are null
- [X] T014 [US1] Create `SubscriptionHistoryController` in `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.API/Controllers/SubscriptionHistoryController.cs` with route `[Route("api/subscription-history")]`; inject `ISubscriptionHistoryService`; add `[HttpGet]` action accepting `[FromQuery] int? page = null` and `[FromQuery] int? pageSize = null`; call `GetAsync` and return `Ok(result)`

**Checkpoint**: `dotnet test` passes T011 and T012. Swagger UI shows `GET /api/subscription-history`. Manual call returns 20 records with null pagination fields.

---

## Phase 4: User Story 2 — Retrieve Paginated Subscription History (Priority: P2)

**Goal**: `GET /api/subscription-history?page=1&pageSize=5` returns the correct slice of records with fully populated `page`, `pageSize`, `totalPages`, and `totalCount` fields.

**Independent Test**: Call `GET /api/subscription-history?page=1&pageSize=5` → verify 200 OK, `items` has 5 records, `totalCount == 20`, `page == 1`, `pageSize == 5`, `totalPages == 4`. Call with `page=0` → verify 400 ProblemDetails.

### Tests for User Story 2

- [X] T015 [P] [US2] Add the following test methods to `Backend/tests/SubscriptionHistory.Application.Tests/SubscriptionHistoryServiceTests.cs`:
  - `GetAsync_WithPageAndPageSize_ReturnsCorrectSlice`: page=1, pageSize=5 → Items.Count==5, TotalCount==20, Page==1, PageSize==5, TotalPages==4
  - `GetAsync_WithPageBeyondLast_ReturnsEmptyItems`: page=99, pageSize=10 → Items.Count==0, TotalCount==20, TotalPages==2
  - `GetAsync_WithPageSizeOnly_DefaultsPageToOne`: pageSize=5 → Page==1, PageSize==5
  - `GetAsync_WithPageOnly_DefaultsPageSizeToTwenty`: page=1 → PageSize==20
  - `GetAsync_WithZeroPage_ThrowsArgumentException`: GetAsync(0, 10) → throws `ArgumentException`
  - `GetAsync_WithZeroPageSize_ThrowsArgumentException`: GetAsync(1, 0) → throws `ArgumentException`
- [X] T016 [P] [US2] Add integration test methods to `Backend/tests/Integration.Tests/SubscriptionHistory/SubscriptionHistoryTests.cs`:
  - `GetSubscriptionHistory_WithValidPagination_Returns200WithMetadata`: GET `?page=1&pageSize=5` → 200, items==5, totalCount==20, page==1, pageSize==5, totalPages==4
  - `GetSubscriptionHistory_WithZeroPage_Returns400ProblemDetails`: GET `?page=0&pageSize=10` → 400, ProblemDetails with detail message
  - `GetSubscriptionHistory_WithZeroPageSize_Returns400ProblemDetails`: GET `?page=1&pageSize=0` → 400, ProblemDetails with detail message
  - `GetSubscriptionHistory_WithPageBeyondTotal_Returns200EmptyItems`: GET `?page=99&pageSize=10` → 200, items==[], totalCount==20

### Implementation for User Story 2

- [X] T017 [US2] Extend `SubscriptionHistoryService.GetAsync` in `Backend/src/Modules/SubscriptionHistory/SubscriptionHistory.Application/Services/SubscriptionHistoryService.cs` to handle pagination:
  - If `page < 1` → throw `ArgumentException("Page number must be greater than 0.")`
  - If `pageSize < 1` → throw `ArgumentException("Page size must be greater than 0.")`
  - When either param is provided: apply effective `page` (default 1) and `pageSize` (default 20); compute `Skip = (page-1) * pageSize`, `Take = pageSize`; return `PagedResponse<SubscriptionHistoryItem>.Paginated(slice, totalCount, effectivePage, effectivePageSize)` — `TotalPages` is computed automatically by the factory

**Checkpoint**: `dotnet test` passes all unit tests (T015) and integration tests (T016). Full `dotnet test` suite shows zero failures.

---

## Phase 5: User Story 3 — Filter Subscription History by Query (Priority: P2)

**Goal**: `GET /api/subscription-history` supports query filtering (`query`) with proper total count behavior.

**Independent Test**: Call `GET /api/subscription-history?query=Alice` and verify only matching rows are returned.

### Tests for User Story 3

- [X] T018 [P] [US3] Add unit tests in `Backend/tests/SubscriptionHistory.Application.Tests/SubscriptionHistoryServiceTests.cs`:
  - `GetAsync_WithQuery_FiltersByClientAccountOrStrategy`

- [X] T019 [P] [US3] Add integration tests in `Backend/tests/Integration.Tests/SubscriptionHistory/SubscriptionHistoryTests.cs`:
  - `GetSubscriptionHistory_WithQueryFilter_ReturnsMatchingRows`

### Implementation for User Story 3

- [X] T020 [US3] Extend `ISubscriptionHistoryService.GetAsync` signature and controller action query parameters to include `query`
- [X] T021 [US3] Extend `SubscriptionHistoryService.GetAsync` to apply filtering before pagination:
  - `query`: case-insensitive partial match on `ClientName`, `AccountNumber`, `StrategyName`

**Checkpoint**: Filtered requests return only matching records and `totalCount` equals filtered set size.

---

## Phase 6: User Story 4 — Sort Subscription History by Field and Direction (Priority: P3)

**Goal**: `GET /api/subscription-history` supports `orderBy` and `orderDirection` (`asc`, `desc`) with validation; sorting is applied after filtering and before pagination.

**Independent Test**: Call `GET /api/subscription-history?orderBy=clientName&orderDirection=asc` and verify ascending sort; invalid sort params return 400.

### Tests for User Story 4

- [X] T022 [P] [US4] Add unit tests in `Backend/tests/SubscriptionHistory.Application.Tests/SubscriptionHistoryServiceTests.cs`:
  - `GetAsync_WithOrderByClientName_DefaultsToDescending`
  - `GetAsync_WithOrderDirectionOnly_AppliesToDefaultTimestamp`
  - `GetAsync_WithInvalidOrderBy_ThrowsArgumentException`
  - `GetAsync_WithInvalidOrderDirection_ThrowsArgumentException`

- [X] T023 [P] [US4] Add integration tests in `Backend/tests/Integration.Tests/SubscriptionHistory/SubscriptionHistoryTests.cs`:
  - `GetSubscriptionHistory_WithOrderByAndDirection_ReturnsSortedRows`
  - `GetSubscriptionHistory_WithInvalidOrderBy_Returns400ProblemDetails`
  - `GetSubscriptionHistory_WithInvalidOrderDirection_Returns400ProblemDetails`

### Implementation for User Story 4

- [X] T024 [US4] Extend service sorting logic with allowed fields:
  - `timestamp`, `clientName`, `accountNumber`, `strategyName`, `equityConnect`
  - default `orderBy=timestamp`, default `orderDirection=desc`
  - if `orderDirection` is provided without `orderBy`, apply it to default field `timestamp`
- [X] T025 [US4] Validate `orderBy` and `orderDirection` values and throw `ArgumentException` for unsupported values

**Checkpoint**: Sorted responses are deterministic and valid combinations with filter/pagination preserve the sequence.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Swagger documentation and final validation.

- [X] T026 [P] Update XML docs for `[HttpGet]` in `SubscriptionHistoryController.cs` to describe all query params (`query`, `orderBy`, `orderDirection`, `page`, `pageSize`)
- [X] T027 Assert `GET /swagger/v1/swagger.json` includes `GET /api/subscription-history` and all five query parameters in the operation definition
- [X] T028 Run full test suite `dotnet test` from `Backend/` and confirm zero failures across all test projects

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — **BLOCKS all user stories**
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion
- **User Story 2 (Phase 4)**: Depends on Phase 3 completion (extends the same service and controller)
- **US3 (Phase 5)**: Depends on Phase 4 completion
- **US4 (Phase 6)**: Depends on Phase 5 completion
- **Polish (Phase 7)**: Depends on Phase 6 completion

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational phase — no dependency on US2
- **US2 (P2)**: Depends on US1 being implemented (US2 extends the same service and controller, not a separate endpoint)
- **US3 (P2)**: Depends on US2 because filtering + pagination interactions must be validated together
- **US4 (P3)**: Depends on US3 because sort is applied after filtering

### Within Each User Story

- Tests (T011–T012, T015–T016) should be written first so they fail before implementation
- DTO and interface (T005, T008) before service implementation (T013, T017)
- Service before controller (T014 depends on T013)
- Implementation before verification

### Parallel Opportunities

- T005, T006, T007 (DTO + GlobalUsings) can run in parallel — different files
- T011 and T012 (US1 tests) can be written in parallel — different files
- T015 and T016 (US2 tests) can be written in parallel — different files
- T018 and T019 (US3 tests) can be written in parallel — different files
- T022 and T023 (US4 tests) can be written in parallel — different files
- T026 (Swagger docs) before T027 (Swagger verification) — sequential

---

## Parallel Example: User Story 1

```
# Write tests in parallel:
Task T011: SubscriptionHistoryServiceTests.cs (unit test)
Task T012: Integration.Tests/SubscriptionHistory/SubscriptionHistoryTests.cs

# After tests are written and failing, implement:
Task T013: SubscriptionHistoryService.cs (mocked data + GetAsync all-records)
Task T014: SubscriptionHistoryController.cs
```

## Parallel Example: User Story 2

```
# Write tests in parallel:
Task T015: SubscriptionHistoryServiceTests.cs (pagination unit tests)
Task T016: Integration.Tests/SubscriptionHistory/SubscriptionHistoryTests.cs (pagination integration tests)

# After tests are written and failing, implement:
Task T017: Extend SubscriptionHistoryService.GetAsync with pagination logic
```

## Parallel Example: User Story 3

```
# Write tests in parallel:
Task T018: SubscriptionHistoryServiceTests.cs (filter unit tests)
Task T019: Integration.Tests/SubscriptionHistory/SubscriptionHistoryTests.cs (filter integration tests)

# After tests are written and failing, implement:
Task T020: Extend controller + service interface parameters
Task T021: Add query filter logic
```

## Parallel Example: User Story 4

```
# Write tests in parallel:
Task T022: SubscriptionHistoryServiceTests.cs (sort unit tests)
Task T023: Integration.Tests/SubscriptionHistory/SubscriptionHistoryTests.cs (sort integration tests)

# After tests are written and failing, implement:
Task T024: Add sorting logic and defaults
Task T025: Add orderBy/orderDirection validation
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T004)
2. Complete Phase 2: Foundational (T005–T010) — **critical blocker**; no custom response DTO — use `PagedResponse<SubscriptionHistoryItem>` from Shared
3. Complete Phase 3: User Story 1 (T011–T014)
4. **STOP and VALIDATE**: `GET /api/subscription-history` returns 20 records
5. Demo if ready

### Incremental Delivery

1. Phase 1 + Phase 2 → Foundation ready
2. Phase 3 → Full list endpoint works → **MVP demo**
3. Phase 4 → Pagination works
4. Phase 5 → Filtering works (`query`)
5. Phase 6 → Sorting works (`orderBy`, `orderDirection`)
6. Phase 7 → Swagger documented and tests green → Ready for PR

---

## Notes

- [P] tasks = different files, no dependencies within that phase
- [Story] label maps each task to its user story for traceability
- US2 extends the same endpoint as US1 — they are not independently deployable as separate routes, but US1 is independently testable by calling without pagination params
- `ExceptionHandlingMiddleware` (already in the codebase) handles `ArgumentException` → 400 ProblemDetails; no new middleware needed
- Mocked data: singleton service lifetime ensures the list is created once and reused across requests
- No EF migrations, no DB setup required — this feature is self-contained
