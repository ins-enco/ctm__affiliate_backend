# Tasks: Generic Paginated Response

**Input**: Design documents from `/specs/003-generic-paged-response/`  
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/ ✅

**Organization**: Single user story — tasks flow from project setup through type implementation to tests.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1 only in this feature)

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create the new `Shared.Tests` project and wire it into the solution before any implementation begins.

- [x] T001 Create `Backend/tests/CopyTradeMarketApi.Shared.Tests/CopyTradeMarketApi.Shared.Tests.csproj` targeting net8.0 with references to `CopyTradeMarketApi.Shared`, `xunit`, `xunit.runner.visualstudio`, and `coverlet.collector` (match the package versions used in the other test projects such as `Auth.Application.Tests`)
- [x] T002 [P] Create `Backend/tests/CopyTradeMarketApi.Shared.Tests/GlobalUsings.cs` with `global using Xunit;` and `global using CopyTradeMarketApi.Shared.Responses;`
- [x] T003 Add `<Project Path="tests/CopyTradeMarketApi.Shared.Tests/CopyTradeMarketApi.Shared.Tests.csproj" />` inside the `<Folder Name="/tests/">` element in `Backend/CopyTradeMarketApi.slnx`

**Checkpoint**: `dotnet build Backend/` succeeds with the new test project included (no implementation yet — empty project is fine).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create the `Responses/` subfolder in Shared. This is a trivial structural step but must precede the type file.

- [x] T004 Create the folder `Backend/src/Shared/CopyTradeMarketApi.Shared/Responses/` (it will contain `PagedResponse.cs`; no code file yet — just verify the path is ready for the next step)

**Checkpoint**: Folder exists. `dotnet build Backend/` still succeeds.

---

## Phase 3: User Story 1 — Any Module Uses the Shared Type (Priority: P1) 🎯 MVP

**Goal**: `PagedResponse<T>` is available in `CopyTradeMarketApi.Shared.Responses`, compiles cleanly, produces correct JSON, and is covered by 7 passing unit tests.

**Independent Test**: Instantiate `PagedResponse<string>.All(new[] { "a", "b" })` in a test → verify `TotalCount == 2`, `Page == null`. Instantiate `PagedResponse<string>.Paginated(slice, 21, 1, 5)` → verify `TotalPages == 5`.

### Implementation for User Story 1

- [x] T005 [US1] Create `Backend/src/Shared/CopyTradeMarketApi.Shared/Responses/PagedResponse.cs` as a positional generic record in namespace `CopyTradeMarketApi.Shared.Responses` with fields `IReadOnlyList<T> Items`, `int TotalCount`, `int? Page`, `int? PageSize`, `int? TotalPages`; add static factory `All(IReadOnlyList<T> items)` returning `new(items, items.Count, null, null, null)`; add static factory `Paginated(IReadOnlyList<T> items, int totalCount, int page, int pageSize)` returning `new(items, totalCount, page, pageSize, (int)Math.Ceiling((double)totalCount / pageSize))`

### Tests for User Story 1

- [x] T006 [US1] Create `Backend/tests/CopyTradeMarketApi.Shared.Tests/Responses/PagedResponseTests.cs` with the following 7 `[Fact]` test methods (xUnit, Arrange/Act/Assert, one assertion focus each):
  - `All_WithItems_ReturnsTotalCountEqualToItemsCount` — `All(new List<string>{"a","b","c"})` → `TotalCount == 3`
  - `All_WithItems_ReturnsNullPaginationFields` — same call → `Page == null`, `PageSize == null`, `TotalPages == null`
  - `All_WithEmptyList_ReturnsZeroTotalCount` — `All(new List<string>())` → `TotalCount == 0`, `Items.Count == 0`
  - `Paginated_WithFullPage_ReturnsCorrectMetadata` — `Paginated(5-item list, 20, 1, 5)` → `Page==1`, `PageSize==5`, `TotalPages==4`, `TotalCount==20`
  - `Paginated_ComputesTotalPagesWithCeiling` — `Paginated(5-item list, 21, 1, 5)` → `TotalPages==5`
  - `Paginated_WithTotalCountZero_ReturnsTotalPagesZero` — `Paginated(empty list, 0, 1, 10)` → `TotalPages==0`
  - `Paginated_WithEmptyPage_ReturnsEmptyItemsButCorrectTotalCount` — `Paginated(empty list, 20, 3, 10)` → `Items.Count==0`, `TotalCount==20`

- [x] T007 [US1] Run `dotnet test Backend/tests/CopyTradeMarketApi.Shared.Tests/` and confirm all 7 tests pass

**Checkpoint**: All 7 unit tests pass. `PagedResponse<T>` is available to any module that references `CopyTradeMarketApi.Shared`.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Full solution validation and spec 004 cross-reference update.

- [x] T008 Run `dotnet test Backend/` to confirm zero failures across all test projects (Auth, Affiliate, Tracking, Integration, Shared)
- [x] T009 [P] Add a note at the top of `specs/004-subscription-history-list/data-model.md` under a `## Dependency on Spec 003` heading stating that `SubscriptionHistoryResponse` is replaced by `PagedResponse<SubscriptionHistoryItem>` from `CopyTradeMarketApi.Shared.Responses` once spec 003 is implemented

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion (folder must exist before file is created)
- **Polish (Phase 4)**: Depends on Phase 3 completion (tests must pass first)

### Within User Story 1

- T005 (type implementation) before T006 (tests) — tests reference the type
- T006 before T007 (run tests) — tests must exist to run
- T002 (GlobalUsings) can run in parallel with T001 and T003 — different files

### Parallel Opportunities

- T001, T002, T003 — all setup tasks target different files; T002 can be written at the same time as T001
- T008 and T009 — independent: test run vs. documentation update

---

## Parallel Example: User Story 1

```
# Phase 1 — run T001 + T002 in parallel (different files):
Task T001: CopyTradeMarketApi.Shared.Tests.csproj
Task T002: CopyTradeMarketApi.Shared.Tests/GlobalUsings.cs

# Phase 3 — sequential (test file depends on type file):
Task T005: PagedResponse.cs  →  Task T006: PagedResponseTests.cs  →  Task T007: dotnet test
```

---

## Implementation Strategy

### MVP First (complete in a single session)

1. Phase 1: Create test project + GlobalUsings + solution entry (T001–T003)
2. Phase 2: Create `Responses/` folder (T004)
3. Phase 3: Implement `PagedResponse<T>` (T005), write tests (T006), run tests (T007)
4. Phase 4: Full suite validation + spec 004 note (T008–T009)

**Total**: 9 tasks — this feature is small enough to complete end-to-end in one sitting.

### Delivery Order

1. `PagedResponse<T>` available in Shared → **Spec 003 done**
2. Spec 004 (Subscription History) can now adopt `PagedResponse<SubscriptionHistoryItem>` during its implementation

---

## Notes

- No Moq needed — `PagedResponse<T>` is a pure record; tests use direct instantiation only
- No EF, no SQLite, no `WebApplicationFactory` — this is a unit-only feature
- `Paginated()` factory does not validate `page`/`pageSize` — caller responsibility (enforced in each module's service)
- Once merged, all future modules referencing `CopyTradeMarketApi.Shared` get `PagedResponse<T>` for free — no additional steps
