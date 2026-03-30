# Tasks: CopyTradeMarket — Milestone 1

Feature: milestone-1
Total tasks: 47
Status: COMPLETED (implemented)

---

## Phase 1 — Setup

- [x] T001 Create solution and project structure per plan.md (src/Host, src/Shared, src/Modules/Auth, Tracking, Affiliate, tests/)
- [x] T002 Add NuGet packages: Serilog, BCrypt, Pomelo EF Core, JwtBearer, xUnit, Moq, WebApplicationFactory
- [x] T003 Create Directory.Packages.props for centralized NuGet version management
- [x] T004 Configure appsettings.json and appsettings.Development.json structure (ConnectionStrings, JwtSettings, ClickTracking, Serilog sections)

---

## Phase 2 — Foundational (Shared Kernel)

- [x] T005 [P] Create BaseEntity with CreatedAt, UpdatedAt (UTC) in CopyTradeMarketApi.Shared
- [x] T006 [P] Create IModule interface (RegisterServices, MapEndpoints) in Shared/Abstractions/
- [x] T007 [P] Create ICacheService interface (GetOrCreateAsync, Remove) in Shared/Abstractions/
- [x] T008 [P] Create IEventPublisher and IEventHandler<T> interfaces in Shared/Abstractions/
- [x] T009 [P] Create ConflictException in Shared/Exceptions/
- [x] T010 [P] Create HashHelper.Sha256() in Shared/Helpers/
- [x] T011 Create MemoryCacheService implementing ICacheService in Shared/Cache/
- [x] T012 Create EventPublisher implementing IEventPublisher (resolves IEventHandler<T> from DI) in Shared/Events/
- [x] T013 Create ExceptionHandlingMiddleware (UnauthorizedAccessException→401, KeyNotFoundException→404, ConflictException→409, InvalidOperationException→400, other→500) in Host/Middleware/
- [x] T014 Wire up Program.cs startup in correct order (Serilog, modules, cache, events, Swagger, JWT, CORS, services, controllers, migration, seeding, middleware)

---

## Phase 3 — US1: User Registration & Login

- [x] T015 [US1] Create User entity in Auth.Domain/Entities/User.cs
- [x] T016 [US1] Create UserByEmailSpecification in Auth.Domain/Specifications/
- [x] T017 [US1] Create BaseSpecification<T> + Apply() IQueryable extension in Shared
- [x] T018 [US1] Create AuthDbContext (Users DbSet, unique index on Email) in Auth.Infrastructure/
- [x] T019 [US1] Create RegisterRequest, LoginRequest, AuthResult records in Auth.Application/DTOs/
- [x] T020 [US1] Create UserRegisteredEvent record in Auth.Application/Events/
- [x] T021 [US1] Create IAffiliateLookupService interface in Shared (CreateAffiliateAsync, GetAffiliateIdByUserIdAsync, FindByCodeAsync, FindByIdAsync)
- [x] T022 [US1] Create IAuthService and AuthService (RegisterAsync: BCrypt hash → save user → CreateAffiliateAsync → publish UserRegisteredEvent → return JWT; LoginAsync: find by email → verify BCrypt → lookup affiliate → return JWT)
- [x] T023 [US1] Create JwtService for JWT token generation (HS256, userId + affiliateId claims)
- [x] T024 [US1] Create AuthController (POST /api/auth/register, POST /api/auth/login) — reads aff_sid cookie and passes SessionId into RegisterRequest
- [x] T025 [US1] Create AuthModule.RegisterServices() and add EF migration for users table

---

## Phase 4 — US2: Click Tracking & Deduplication

- [x] T026 [US2] Create ClickEvent entity (BIGINT Id) in Tracking.Domain/Entities/
- [x] T027 [US2] Create all 7 Tracking specifications in Tracking.Domain/Specifications/:
  - ClickByAffiliateAndSessionSpecification
  - LatestClickBySessionSpecification
  - ConversionBySessionAndTypeSpecification
  - ClicksByAffiliateSpecification
  - RecentClicksSpecification
  - ClickWithConversionSpecification
- [x] T028 [US2] Create TrackingDbContext (ClickEvents DbSet, UNIQUE index on AffiliateId+SessionId, INDEX on ClickedAt) in Tracking.Infrastructure/
- [x] T029 [US2] Create ClickResult, ConversionRequest, ConversionResult, ClickStats records in Tracking.Application/DTOs/
- [x] T030 [US2] Create ITrackingService and TrackingService.RecordClickAsync():
  - Lookup affiliate via cache (10-min TTL)
  - Use existing sessionId from cookie or compute SHA-256(IP+UA+code+bucket)
  - GetAttributionBucket() → protected virtual → DateTime.UtcNow.ToString("yyyy-MM")
  - Save click; catch DbUpdateException for duplicate → IsUnique = false
  - Invalidate cache on success
  - Return ClickResult with SessionId
- [x] T031 [US2] Create TrackingController.RecordClick() — reads aff_sid cookie, calls service, sets aff_sid cookie to result.SessionId
- [x] T032 [US2] Add EF migration for click_events table

---

## Phase 5 — US3: Conversion Attribution + Observer Pattern

- [x] T033 [US3] Create ConversionEvent entity (BIGINT Id) in Tracking.Domain/Entities/
- [x] T034 [US3] Update TrackingDbContext with ConversionEvents DbSet and indexes
- [x] T035 [US3] Create TrackingService.RecordConversionAsync():
  - Validate ConversionType ∈ {Registration, Deposit} → throw InvalidOperationException
  - Check duplicate via ConversionBySessionAndTypeSpecification → throw ConflictException
  - Find latest click via LatestClickBySessionSpecification → get AffiliateId (0 if none)
  - Save ConversionEvent; return ConversionResult
- [x] T036 [US3] Create TrackingController.RecordConversion() (POST /api/tracking/convert)
- [x] T037 [US3] Create UserRegisteredEventHandler in Tracking.Application/:
  - HandleAsync: if SessionId is null → return immediately
  - Call TrackingService.RecordConversionAsync(sessionId, "Registration", userId)
- [x] T038 [US3] Register UserRegisteredEventHandler as IEventHandler<UserRegisteredEvent> in TrackingModule.RegisterServices()
- [x] T039 [US3] Add EF migration for conversion_events table (AddConversionEvents)

---

## Phase 6 — US4: Affiliate Dashboard

- [x] T040 [US4] Create Affiliate entity in Affiliate.Domain/Entities/
- [x] T041 [US4] Create AffiliateByCodeSpecification, AffiliateByIdSpecification, AffiliateByUserIdSpecification in Affiliate.Domain/Specifications/
- [x] T042 [US4] Create AffiliateDbContext (Affiliates DbSet, UNIQUE on UniqueCode) in Affiliate.Infrastructure/
- [x] T043 [US4] Create AffiliateLookupService implementing IAffiliateLookupService:
  - CreateAffiliateAsync: generate unique 8-char code (retry until unique) → save → return affiliateId + code
  - GetAffiliateIdByUserIdAsync, FindByCodeAsync, FindByIdAsync
- [x] T044 [US4] Create IClickStatsReader and ClickStatsReader in Tracking.Application/ (TotalClicks, UniqueClicks, Last7DayClicks, ConvertedClicks via specifications)
- [x] T045 [US4] Create AffiliateDashboardService.GetDashboardAsync():
  - Load affiliate by ID → 404 if missing
  - Read click stats via IClickStatsReader
  - GetOrCreate cache ("affiliate:clickcount:{id}", 5-min TTL)
  - Return DashboardResult
- [x] T046 [US4] Create AffiliateController (GET /api/affiliate/dashboard, [Authorize])
- [x] T047 [US4] Create AffiliateModule.RegisterServices() and add EF migration for affiliates table

---

## Final Phase — Polish & Testing

- [x] T048 Create DevDataSeeder (alice, bob, carol accounts with seed clicks/conversions) in Host/
- [x] T049 [P] Write unit tests: Auth.Application.Tests (AuthService — 6 cases)
- [x] T050 [P] Write unit tests: Affiliate.Application.Tests (AffiliateLookupService — 7 cases, AffiliateDashboardService — 2 cases)
- [x] T051 [P] Write unit tests: Tracking.Application.Tests (TrackingService — 9 cases, UserRegisteredEventHandler — 3 cases, ClickStatsReader — 3 cases)
- [x] T052 Create IntegrationWebFactory (env=Testing, SQLite in-memory for all 3 DbContexts, JwtSettings + JwtBearerOptions override, EnsureCreated)
- [x] T053 Write FullJourneyTests (5 end-to-end flows + 4 error cases)
- [x] T054 Write ObserverPatternTests (register with/without aff_sid cookie)
- [x] T055 Write AttributionWindowTests (same bucket → duplicate; new bucket → unique) using BucketOverrideTrackingService
- [x] T056 Write SeededScenarioTests using SeededIntegrationFactory + TestDataSeeder (7 cases)
- [x] T057 Create StressWebFactory + stress test runner (SemaphoreSlim concurrency model, ResourceMonitor, HTML report with SVG charts)
- [x] T058 Create Dockerfile + docker-compose.yml (api, db, frontend profile)

---

## Dependencies

```
Phase 1 (Setup)
  → Phase 2 (Shared Kernel)
    → Phase 3 (Auth) + Phase 4 (Tracking click) + Phase 6 (Affiliate)
      → Phase 5 (Conversion + Observer) [requires Auth event + Tracking service]
        → Final Phase (Tests)
```

Parallel opportunities within phases: T005-T010 (Shared), T049-T051 (unit tests), T052-T058 (integration + stress tests).

---

## Implementation Strategy

MVP scope (minimum to demonstrate the full flow):
1. T001–T014 (Setup + Shared)
2. T015–T025 (Auth)
3. T040–T047 (Affiliate — needed for click lookup)
4. T026–T032 (Click tracking)
5. T033–T039 (Conversion + Observer)
6. T046 (Dashboard)

Then tests last (T048–T058).
