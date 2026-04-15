# Tasks: Mock Module — Dashboard API

**Input**: Design documents from `specs/005-mock-dashboard-api/`  
**Branch**: `feature/005-mock-dashboard-api`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Included — spec acceptance scenarios require unit and integration test coverage (SC-002, SC-004, SC-006; constitution Definition of Done).

**Organization**: Grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no shared dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5)
- Exact file paths included in every description

## Key Design Decisions (from research.md)

- Two-layer module: `Mock.API` + `Mock.Application` (no Domain/Infrastructure)
- Single `MockController` with 5 separate `[HttpGet]` actions at route `api/mock`
- Plain `List<T>` responses — no `PagedResponse<T>` wrapper
- Singleton DI lifetime for `MockService`
- **DEV-only (FR-011)**: Module registered only when `builder.Environment.IsDevelopment()` is true
- Integration tests use a new `MockWebFactory` (Development env) for happy-path tests; existing `IntegrationWebFactory` ("Testing" env) covers the non-Dev 404 test — no new factory needed for SC-006

---

## Phase 1: Setup

**Purpose**: Create the three new projects and wire the host reference

- [X] T001 Create `Backend/src/Modules/Mock/Mock.Application/Mock.Application.csproj` — `net8.0`, `ImplicitUsings enable`, `Nullable enable`; `<ProjectReference>` to `CopyTradeMarketApi.Shared`
- [X] T002 [P] Create `Backend/src/Modules/Mock/Mock.API/Mock.API.csproj` — `net8.0`, `ImplicitUsings enable`, `Nullable enable`, `GenerateDocumentationFile true`; `<FrameworkReference Include="Microsoft.AspNetCore.App"/>`; `<ProjectReference>` to `Mock.Application`
- [X] T003 [P] Create `Backend/tests/Mock.Application.Tests/Mock.Application.Tests.csproj` — `net8.0`, `IsPackable false`; packages: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`; `<ProjectReference>` to `Mock.Application`
- [X] T004 Add `<ProjectReference Include="..\..\Modules\Mock\Mock.API\Mock.API.csproj" />` to `Backend/src/Host/CopyTradeMarketApi.Host/CopyTradeMarketApi.Host.csproj`
- [X] T005 [P] Create `Backend/src/Modules/Mock/Mock.Application/GlobalUsings.cs` — `global using Mock.Application.DTOs;`
- [X] T006 [P] Create `Backend/src/Modules/Mock/Mock.API/GlobalUsings.cs` — global usings: `Microsoft.AspNetCore.Builder`, `Microsoft.AspNetCore.Mvc`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection`, `Mock.Application.Services`, `CopyTradeMarketApi.Shared.Abstractions`
- [X] T007 [P] Create `Backend/tests/Mock.Application.Tests/GlobalUsings.cs` — `global using Mock.Application.Services;` and `global using Mock.Application.DTOs;`

**Checkpoint**: Three new projects compile. Host references `Mock.API`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: DTOs + service interface + skeleton implementation + controller + module + host wiring + Dev-env test factory

**⚠️ CRITICAL**: All user story work and integration tests depend on this phase being complete.

- [X] T008 [P] Create `Backend/src/Modules/Mock/Mock.Application/DTOs/UserDto.cs` — `public record UserDto(int Id, string Name, string Role);`
- [X] T009 [P] Create `Backend/src/Modules/Mock/Mock.Application/DTOs/CurrentUserDto.cs` — `public record CurrentUserDto(int Id, string Name, string Abbreviation, string Role);`
- [X] T010 [P] Create `Backend/src/Modules/Mock/Mock.Application/DTOs/ClientRequestDto.cs` — `public record ClientRequestDto(DateTime Timestamp, string Name, decimal Equity, string Strategy, string StrategyLicense);`
- [X] T011 [P] Create `Backend/src/Modules/Mock/Mock.Application/DTOs/SignalProviderRequestDto.cs` — `public record SignalProviderRequestDto(DateTime Timestamp, string Name, string KycStatus);`
- [X] T012 [P] Create `Backend/src/Modules/Mock/Mock.Application/DTOs/AffiliateRequestDto.cs` — `public record AffiliateRequestDto(DateTime Timestamp, string Name, string KycStatus);`
- [X] T013 Create `Backend/src/Modules/Mock/Mock.Application/Services/IMockService.cs` — interface with 5 methods: `Task<List<UserDto>> GetUsersAsync()`, `Task<CurrentUserDto> GetCurrentUserAsync()`, `Task<List<ClientRequestDto>> GetClientRequestsAsync()`, `Task<List<SignalProviderRequestDto>> GetSignalProviderRequestsAsync()`, `Task<List<AffiliateRequestDto>> GetAffiliateRequestsAsync()`
- [X] T014 Create `Backend/src/Modules/Mock/Mock.Application/Services/MockService.cs` — skeleton `public class MockService : IMockService` with all 5 methods returning `Task.FromResult(new List<...>())` or `Task.FromResult<CurrentUserDto>(null!)` (stubs; full data added per story)
- [X] T015 Create `Backend/src/Modules/Mock/Mock.API/MockModule.cs` — `public class MockModule : IModule` with `RegisterServices` calling `services.AddSingleton<IMockService, MockService>()` and empty `MapEndpoints`
- [X] T016 Create `Backend/src/Modules/Mock/Mock.API/Controllers/MockController.cs` — `[ApiController] [Route("api/mock")]` with primary constructor `(IMockService service)`; 5 stub actions: `[HttpGet("users")]`, `[HttpGet("current-user")]`, `[HttpGet("client-requests")]`, `[HttpGet("signal-provider-requests")]`, `[HttpGet("affiliate-requests")]` — each calls the matching service method and returns `Ok(result)`
- [X] T017 Wire `MockModule` into `Backend/src/Host/CopyTradeMarketApi.Host/Program.cs`: (1) capture `IMvcBuilder` from `AddControllers()` call; (2) conditionally add `modules.Add(new MockModule())` and `mvcBuilder.AddApplicationPart(typeof(MockModule).Assembly)` inside `if (builder.Environment.IsDevelopment())` block — before `builder.Build()`
- [X] T018 [P] Create `Backend/tests/Integration.Tests/Mock/MockWebFactory.cs` — `public class MockWebFactory : WebApplicationFactory<Program>` with `UseEnvironment("Development")` and the same `ConfigureAppConfiguration` + `ConfigureTestServices` (SQLite DbContexts, JwtSettings override, JWT Bearer PostConfigure) as `IntegrationWebFactory`, plus `CreateHost` that calls `EnsureCreated()` on all three DbContexts

**Checkpoint**: `dotnet build Backend/` succeeds. All 5 routes respond (with stub/empty data) when host runs in Development. Non-Development calls return 404 (module not registered).

---

## Phase 3: User Story 1 — User List (Priority: P1) 🎯 MVP

**Goal**: `GET /api/mock/users` returns ≥5 users covering all three roles.

**Independent Test**: Call `GET /api/mock/users` with no parameters. Verify 200, array length ≥5, and the role values `Client`, `Signal Provider`, `Affiliate` each appear at least once.

- [X] T019 [US1] Implement `GetUsersAsync()` in `Backend/src/Modules/Mock/Mock.Application/Services/MockService.cs` — private static readonly `List<UserDto>` with ≥5 entries covering all three role values; method returns `Task.FromResult(_users)`
- [X] T020 [P] [US1] Create `Backend/tests/Mock.Application.Tests/MockServiceTests.cs` with unit tests for `GetUsersAsync`: (a) count ≥5; (b) all three roles present (`Client`, `Signal Provider`, `Affiliate`); (c) every `Role` is in the allowed set
- [X] T021 [P] [US1] Create `Backend/tests/Integration.Tests/Mock/MockTests.cs` with `IClassFixture<MockWebFactory>`; test: `GET /api/mock/users` → 200, body deserializes as `List<UserDto>` (not null), count ≥5, all three roles present

**Checkpoint**: US1 fully functional and independently testable. `dotnet test` for `Mock.Application.Tests` and `Integration.Tests --filter Mock` passes.

---

## Phase 4: User Story 2 — Current Active User (Priority: P1)

**Goal**: `GET /api/mock/current-user` returns a single object with a 2-character abbreviation.

**Independent Test**: Call `GET /api/mock/current-user`. Verify 200, response is a single JSON object (not an array), `abbreviation` field is exactly 2 characters, `role` is from the allowed set.

- [X] T022 [US2] Implement `GetCurrentUserAsync()` in `MockService.cs` — private static readonly `CurrentUserDto` instance with `id`, `name`, `abbreviation` (exactly 2 uppercase chars — e.g., `"CS"` for `"Carlos Silva"`), and `role`; method returns `Task.FromResult(_currentUser)`
- [X] T023 [P] [US2] Add unit tests for `GetCurrentUserAsync` to `MockServiceTests.cs`: (a) returns non-null; (b) `Abbreviation.Length == 2`; (c) `Role` is in `["Client","Signal Provider","Affiliate"]`
- [X] T024 [P] [US2] Add integration test to `MockTests.cs`: `GET /api/mock/current-user` → 200; deserialize as `CurrentUserDto`; `Abbreviation.Length == 2`

**Checkpoint**: US2 fully functional and independently testable.

---

## Phase 5: User Story 3 — Client Requests (Priority: P2)

**Goal**: `GET /api/mock/client-requests` returns exactly 10 records with positive equity.

**Independent Test**: Call `GET /api/mock/client-requests`. Verify 200, exactly 10 records, all `equity` values > 0, all fields non-null.

- [X] T025 [US3] Implement `GetClientRequestsAsync()` in `MockService.cs` — private static readonly `List<ClientRequestDto>` with exactly 10 entries; all `Equity` values > 0 decimal; all `Timestamp` values `DateTime` with `DateTimeKind.Utc`
- [X] T026 [P] [US3] Add unit tests for `GetClientRequestsAsync` to `MockServiceTests.cs`: (a) count == 10; (b) every `Equity > 0`; (c) every record has non-empty `Name`, `Strategy`, `StrategyLicense`
- [X] T027 [P] [US3] Add integration test to `MockTests.cs`: `GET /api/mock/client-requests` → 200; count == 10; every `equity > 0`

**Checkpoint**: US3 fully functional and independently testable.

---

## Phase 6: User Story 4 — Signal Provider Requests (Priority: P2)

**Goal**: `GET /api/mock/signal-provider-requests` returns exactly 10 records with valid KYC statuses.

**Independent Test**: Call `GET /api/mock/signal-provider-requests`. Verify 200, exactly 10 records, every `kycStatus` is one of `Pending`, `Verified`, `Rejected`.

- [X] T028 [US4] Implement `GetSignalProviderRequestsAsync()` in `MockService.cs` — private static readonly `List<SignalProviderRequestDto>` with exactly 10 entries; every `KycStatus` ∈ `{"Pending","Verified","Rejected"}`; timestamps UTC
- [X] T029 [P] [US4] Add unit tests for `GetSignalProviderRequestsAsync` to `MockServiceTests.cs`: (a) count == 10; (b) every `KycStatus` in allowed set; (c) every record has non-empty `Name`
- [X] T030 [P] [US4] Add integration test to `MockTests.cs`: `GET /api/mock/signal-provider-requests` → 200; count == 10; every `kycStatus` in `["Pending","Verified","Rejected"]`

**Checkpoint**: US4 fully functional and independently testable.

---

## Phase 7: User Story 5 — Affiliate Requests (Priority: P2)

**Goal**: `GET /api/mock/affiliate-requests` returns exactly 10 records with valid KYC statuses.

**Independent Test**: Call `GET /api/mock/affiliate-requests`. Verify 200, exactly 10 records, every `kycStatus` is one of `Pending`, `Verified`, `Rejected`.

- [X] T031 [US5] Implement `GetAffiliateRequestsAsync()` in `MockService.cs` — private static readonly `List<AffiliateRequestDto>` with exactly 10 entries; every `KycStatus` ∈ `{"Pending","Verified","Rejected"}`; timestamps UTC
- [X] T032 [P] [US5] Add unit tests for `GetAffiliateRequestsAsync` to `MockServiceTests.cs`: (a) count == 10; (b) every `KycStatus` in allowed set; (c) every record has non-empty `Name`
- [X] T033 [P] [US5] Add integration test to `MockTests.cs`: `GET /api/mock/affiliate-requests` → 200; count == 10; every `kycStatus` in `["Pending","Verified","Rejected"]`

**Checkpoint**: All 5 user stories functional. All unit and integration tests for US1–US5 pass.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Swagger docs, environment-gating test (FR-011 / SC-006), full test run

- [X] T034 [P] Add XML `<summary>` and `<returns>` doc comments to all 5 action methods in `Backend/src/Modules/Mock/Mock.API/Controllers/MockController.cs` (Swagger UI description)
- [X] T035 [P] Add integration test to `MockTests.cs` using `MockWebFactory` (Development env): Swagger JSON at `GET /swagger/v1/swagger.json` → 200; `paths` object contains all 5 keys: `/api/mock/users`, `/api/mock/current-user`, `/api/mock/client-requests`, `/api/mock/signal-provider-requests`, `/api/mock/affiliate-requests`
- [X] T036 [P] Add non-Dev environment test to `MockTests.cs`: use `IntegrationWebFactory` (which sets `"Testing"` environment — not Development); `GET /api/mock/users` → 404 (FR-011: module not registered outside Development); name the class/region `MockNonDevTests`
- [X] T037 Run `dotnet test` from `Backend/` and confirm all tests pass; resolve any compilation or assertion failures before marking complete

---

## Phase 9: CR-22 Route & Contract Alignment

**Purpose**: Align routes, DTO id types, service signatures, and tests with the CR-22 contract change.

**⚠️ PREREQUISITE**: T038–T040 (DTO + interface changes) must be applied before T041–T044 to avoid compilation failures.

- [X] T038 [P] Update `Backend/src/Modules/Mock/Mock.Application/DTOs/UserDto.cs` — change `int Id` to `string Id`: `public record UserDto(string Id, string Name, string Role);`
- [X] T039 [P] Update `Backend/src/Modules/Mock/Mock.Application/DTOs/CurrentUserDto.cs` — change `int Id` to `string Id`: `public record CurrentUserDto(string Id, string Name, string Abbreviation, string Role);`
- [X] T040 Update `Backend/src/Modules/Mock/Mock.Application/Services/IMockService.cs` — update method signatures: `Task<PagedResponse<UserDto>> GetUsersAsync(string? searchText = null)` and `Task<CurrentUserDto> GetCurrentUserAsync(string userId)` (other 3 methods unchanged)
- [X] T041 Update `Backend/src/Modules/Mock/Mock.Application/Services/MockService.cs`:
  - Change all `int` Id literal values in the static `_users` and `_currentUser` fields to `string` (e.g. `"1"`, `"2"`, etc.)
  - Update `GetUsersAsync()` signature to `GetUsersAsync(string? searchText = null)` — when `searchText` is non-null and non-whitespace, filter `_users` using `user.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)`; otherwise return the full list
  - Update `GetCurrentUserAsync()` signature to `GetCurrentUserAsync(string userId)` — parameter is accepted for interface compliance; the method still returns the single static `_currentUser` record on every call
- [X] T042 Refactor `Backend/src/Modules/Mock/Mock.API/Controllers/MockController.cs`:
  - Replace `[Route("api/mock")]` on the controller with `[Route("api")]`
  - Replace `[HttpGet("users")]` with `[HttpGet("dashboard/listOfUsers")]`; update parameter to `[FromQuery] string? searchText = null`; forward `searchText` to `_service.GetUsersAsync(searchText)`
  - Replace `[HttpGet("current-user")]` with `[HttpGet("currentActiveUser/{userId}")]`; add `[FromRoute] string userId` parameter; forward to `_service.GetCurrentUserAsync(userId)`
  - Replace `[HttpGet("client-requests")]` with `[HttpGet("dashboard/clientRequests")]`
  - Replace `[HttpGet("signal-provider-requests")]` with `[HttpGet("dashboard/signalProviderRequests")]`
  - Replace `[HttpGet("affiliate-requests")]` with `[HttpGet("dashboard/affiliateRequests")]`
  - Update XML `<summary>` doc comments on all 5 actions to reflect the new purpose/routes
- [X] T043 Update unit tests in `Backend/tests/Mock.Application.Tests/MockServiceTests.cs`:
  - Update all `_service.GetUsersAsync()` calls to `_service.GetUsersAsync(null)` (or add `searchText: null` named arg)
  - Update all `_service.GetCurrentUserAsync()` calls to `_service.GetCurrentUserAsync("1")` (using a valid mock user id)
  - Add test: `GetUsersAsync(searchText: "nam")` returns only users whose name contains "nam" (case-insensitive)
  - Add test: `GetUsersAsync(searchText: null)` and `GetUsersAsync(searchText: "")` both return the full user list
- [X] T044 Update integration tests in `Backend/tests/Integration.Tests/Mock/MockTests.cs`:
  - Replace `/api/mock/users` with `/api/dashboard/listOfUsers` in all test HTTP calls
  - Replace `/api/mock/current-user` with `/api/currentActiveUser/1` (use the first mock user's string id) in all test HTTP calls
  - Replace `/api/mock/client-requests` with `/api/dashboard/clientRequests`
  - Replace `/api/mock/signal-provider-requests` with `/api/dashboard/signalProviderRequests`
  - Replace `/api/mock/affiliate-requests` with `/api/dashboard/affiliateRequests`
  - Update T035 Swagger path assertion keys: replace `/api/mock/users`, `/api/mock/current-user`, etc. with the 5 new path strings
  - Update T036 non-Dev test URL from `/api/mock/users` to `/api/dashboard/listOfUsers`
  - Add integration test: `GET /api/dashboard/listOfUsers?searchText={partialName}` → 200; returned users all have names containing the search text
- [X] T045 Run `dotnet test` from `Backend/` and confirm all tests pass; resolve any compilation or assertion failures before marking complete

**Checkpoint**: All Phase 9 tasks complete. Routes match CR-22. `dotnet test` passes with no failures or warnings.

---

## Phase 10: Generic Response Wrapping (`PagedResponse<T>`)

**Purpose**: Wrap all four list endpoints in `PagedResponse<T>.All()` so responses follow the same envelope contract as `SubscriptionHistory`. The single-object `currentActiveUser` endpoint is intentionally NOT wrapped (it returns a plain `CurrentUserDto`).

- [X] T046 [P] Add `global using CopyTradeMarketApi.Shared.Responses;` to `Backend/src/Modules/Mock/Mock.Application/GlobalUsings.cs`
- [X] T047 [P] Update `Backend/src/Modules/Mock/Mock.Application/Services/IMockService.cs` — change return types: `GetUsersAsync` → `Task<PagedResponse<UserDto>>`, `GetClientRequestsAsync` → `Task<PagedResponse<ClientRequestDto>>`, `GetSignalProviderRequestsAsync` → `Task<PagedResponse<SignalProviderRequestDto>>`, `GetAffiliateRequestsAsync` → `Task<PagedResponse<AffiliateRequestDto>>`; `GetCurrentUserAsync` remains `Task<CurrentUserDto>`
- [X] T048 Update `Backend/src/Modules/Mock/Mock.Application/Services/MockService.cs` — update the 4 list method return types to match the interface; each returns `Task.FromResult(PagedResponse<T>.All(<list>))` instead of `Task.FromResult(<list>)`
- [X] T049 Update unit tests in `Backend/tests/Mock.Application.Tests/MockServiceTests.cs`: deserialize result as `PagedResponse<T>` and assert on `.Items`/`.TotalCount`; e.g. `result.Items.Count >= 5`, `result.TotalCount >= 5`, `result.Page == null`, `result.PageSize == null`
- [X] T050 Update integration tests in `Backend/tests/Integration.Tests/Mock/MockTests.cs`: deserialize list responses as `PagedResponse<T>` and assert on `.Items` and `.TotalCount` instead of directly on the array
- [X] T051 Run `dotnet test` from `Backend/` and confirm all tests pass

**Checkpoint**: All list endpoints return `{ items: [...], totalCount: N, page: null, pageSize: null, totalPages: null }`. `currentActiveUser` continues to return a plain object. `dotnet test` passes.

---

## Phase 11: API-KEY Header Authentication for `currentActiveUser`

**Purpose**: Remove the `{userId}` path parameter from `GET /api/currentActiveUser`; protect the endpoint with a dev-only `API-KEY` header check that returns 401 when the header is absent or has an unexpected value. The filter is retained in Production code but never executes (endpoint not registered outside Development — FR-011).

**⚠️ PREREQUISITE**: T052–T053 (interface + service change) must land before T054–T055 (controller + filter) to avoid compilation failures.

- [X] T052 [P] Update `Backend/src/Modules/Mock/Mock.Application/Services/IMockService.cs` — change `Task<CurrentUserDto> GetCurrentUserAsync(string userId)` to `Task<CurrentUserDto> GetCurrentUserAsync()` (remove `userId` parameter entirely)
- [X] T053 [P] Update `Backend/src/Modules/Mock/Mock.Application/Services/MockService.cs` — change `public Task<CurrentUserDto> GetCurrentUserAsync(string userId)` to `public Task<CurrentUserDto> GetCurrentUserAsync()`; method body remains `Task.FromResult(_currentUser)`
- [X] T054 Create `Backend/src/Shared/CopyTradeMarketApi.Shared/Filters/DevApiKeyFilter.cs` (shared library, not Mock-specific — reusable across all modules):
  ```csharp
  namespace CopyTradeMarketApi.Shared.Filters;

  public class DevApiKeyFilter(IWebHostEnvironment env) : IActionFilter
  {
      private const string HeaderName = "API-KEY";
      private const string ValidKey   = "SimulatedKeyForDev";

      public void OnActionExecuting(ActionExecutingContext context)
      {
          if (!env.IsDevelopment()) return;
          if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key)
              || key != ValidKey)
              context.Result = new UnauthorizedResult();
      }

      public void OnActionExecuted(ActionExecutedContext context) { }
  }
  ```
- [X] T055 Update `Backend/src/Modules/Mock/Mock.API/Controllers/MockController.cs`:
  - Change `[HttpGet("currentActiveUser/{userId}")]` to `[HttpGet("currentActiveUser")]`
  - Remove `[FromRoute] string userId` parameter from the action
  - Add `[ServiceFilter(typeof(DevApiKeyFilter))]` attribute on that action
  - Change `service.GetCurrentUserAsync(userId)` to `service.GetCurrentUserAsync()`
- [X] T056 Update `Backend/src/Modules/Mock/Mock.API/MockModule.cs` — add `services.AddScoped<DevApiKeyFilter>();` inside `RegisterServices` so the `ServiceFilter` can resolve it
- [X] T057 Update `Backend/src/Modules/Mock/Mock.API/GlobalUsings.cs` — add `global using CopyTradeMarketApi.Shared.Filters;` (replaces earlier `global using Mock.API.Filters;` since filter moved to Shared)
- [X] T058 Update unit tests in `Backend/tests/Mock.Application.Tests/MockServiceTests.cs` — change all 4 occurrences of `_service.GetCurrentUserAsync("1")` to `_service.GetCurrentUserAsync()` (no argument)
- [X] T059 Update integration tests in `Backend/tests/Integration.Tests/Mock/MockTests.cs` (US2 happy-path class):
  - Change all `GET /api/currentActiveUser/1` calls to `GET /api/currentActiveUser`
  - Add `"API-KEY"` header with value `"SimulatedKeyForDev"` to all US2 happy-path requests (per-request via `HttpRequestMessage`)
  - Added test: `GET /api/currentActiveUser` WITHOUT the `API-KEY` header → 401 Unauthorized
  - Added test: `GET /api/currentActiveUser` with `API-KEY: wrong-key` → 401 Unauthorized
- [X] T060 Run `dotnet test` from `Backend/` — 207 tests pass, 0 failures

**Checkpoint**: `GET /api/currentActiveUser` (no path param) returns 200 with current user when `API-KEY: SimulatedKeyForDev` is sent in Development. Returns 401 when header is absent or incorrect. `dotnet test` passes with all tests green.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — **BLOCKS all user story phases and integration tests**
- **Phase 3–7 (User Stories)**: All depend on Phase 2; can proceed in priority order (US1, US2 first as P1; then US3–US5 as P2)
- **Phase 8 (Polish)**: Depends on all user story phases complete

### User Story Dependencies

- **US1 (P1)**: No dependencies on other stories
- **US2 (P1)**: No dependencies on other stories (can parallel with US1 after Phase 2)
- **US3 (P2)**: No dependencies on other stories
- **US4 (P2)**: No dependencies on other stories (can parallel with US3 after Phase 2)
- **US5 (P2)**: No dependencies on other stories (can parallel with US3/US4 after Phase 2)

### Within Each User Story

- Implementation (T0xx) before tests when tests assert against real data
- Unit tests [P] and integration tests [P] within a story can be written together

### Parallel Opportunities

- T002, T003 — parallel with T001 (separate project files)
- T005, T006, T007 — parallel with each other (separate files)
- T008–T012 — fully parallel (5 separate DTO files)
- T018 — parallel with T013–T017 (separate test file, no dependency on service impl)
- T020, T021 — parallel with each other after T019
- T023, T024 — parallel after T022
- US3/US4/US5 implementation tasks — parallel after Phase 2

---

## Parallel Example: Phase 2 (DTOs)

```
Run all 5 DTO tasks in parallel:
  T008: UserDto.cs
  T009: CurrentUserDto.cs
  T010: ClientRequestDto.cs
  T011: SignalProviderRequestDto.cs
  T012: AffiliateRequestDto.cs
```

## Parallel Example: User Story 1

```
After T019 (GetUsersAsync implementation):
  T020: Unit tests for GetUsersAsync
  T021: Integration test GET /api/mock/users
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (blocks everything)
3. Complete Phase 3: US1 (User List)
4. **STOP and VALIDATE**: `dotnet test --filter Mock` passes; `GET /api/mock/users` returns expected data
5. Demo / handoff frontend integration

### Incremental Delivery

1. Setup + Foundational → host compiles, stubs return empty results
2. US1 → user list works → demo
3. US2 → current user works → dashboard header renders
4. US3 → client requests works → first list panel renders
5. US4 + US5 → remaining panels render
6. Phase 8 → Swagger docs + environment gating verified

### Parallel Team Strategy

With multiple developers (after Phase 2 complete):
- Developer A: US1 + US2 (P1 stories)
- Developer B: US3 + US4 + US5 (P2 stories, sequential or parallel)
- Phase 8: Together after all stories merge

---

## Notes

- [P] = different files, no incomplete task dependencies
- [Story] label maps each task to the user story for traceability
- `MockWebFactory` is required for all happy-path integration tests — the existing `IntegrationWebFactory` uses `"Testing"` env (not Development), so mock endpoints would return 404 without the new factory
- `IntegrationWebFactory` ("Testing" env) doubles as the non-Dev factory for FR-011 / SC-006 coverage — no third factory needed
- All static mock data is hardcoded in `MockService.cs` as private static readonly fields — deterministic on every call (spec edge case)
- Commit after each phase checkpoint to keep history clean
