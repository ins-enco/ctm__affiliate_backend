# Implementation Plan: CopyTradeMarket — Milestone 1

## Tech Stack

| Layer | Choice |
|---|---|
| Runtime | .NET 8 / ASP.NET Core Web API |
| Database | MySQL 8.0 via Pomelo EF Core |
| Authentication | JWT Bearer (HS256) |
| Logging | Serilog — Console + daily rolling file |
| Cache | `ICacheService` abstraction → `MemoryCacheService` (in-memory) |
| Password | BCrypt.Net-Next (cost factor 12) |
| Session hashing | SHA-256 (HashHelper) |

---

## Architecture

**Pattern:** Clean Architecture + Modular Monolith

Three self-contained modules (Auth, Tracking, Affiliate), each with four layers:
- `{Module}.Domain` — Entities, Specifications, domain rules (no dependencies)
- `{Module}.Application` — Services, DTOs, business logic, interface contracts
- `{Module}.Infrastructure` — DbContext, EF config, migrations
- `{Module}.API` — Controllers, `IModule` registration

**Shared Kernel** (`CopyTradeMarketApi.Shared`): base types, helpers, cross-module interfaces.

**Rule:** Modules never reference each other's assemblies. Cross-module calls go through `Shared` interfaces only.

---

## Constitution Check

- [x] P1 — Modules Are Islands: Each module has its own DbContext; cross-module calls via `IAffiliateLookupService` / `IEventPublisher`
- [x] P2 — Specification Pattern: All queries use named `BaseSpecification<T>` + `Apply()` extension
- [x] P3 — Domain Events: `UserRegisteredEvent` (Auth) → `UserRegisteredEventHandler` (Tracking) via `IEventPublisher`
- [x] P4 — Secrets Never In Source: Connection string and JWT key via User Secrets / env vars
- [x] P5 — Async All the Way: All EF and service calls use `*Async()` variants
- [x] P6 — Consistent Error Contract: All failures throw typed exceptions → RFC 7807 ProblemDetails via middleware

---

## Project Structure

```
CopyTradeMarketApi/
├── src/
│   ├── Host/
│   │   └── CopyTradeMarketApi.Host/
│   │       ├── Program.cs
│   │       ├── appsettings.json
│   │       ├── appsettings.Development.json
│   │       ├── DevDataSeeder.cs
│   │       └── Middleware/
│   │           └── ExceptionHandlingMiddleware.cs
│   ├── Shared/
│   │   └── CopyTradeMarketApi.Shared/
│   │       ├── Abstractions/
│   │       │   ├── IModule.cs
│   │       │   ├── ICacheService.cs
│   │       │   ├── IEventPublisher.cs
│   │       │   └── IEventHandler.cs
│   │       ├── Cache/
│   │       │   └── MemoryCacheService.cs
│   │       ├── Events/
│   │       │   └── EventPublisher.cs
│   │       ├── Exceptions/
│   │       │   └── ConflictException.cs
│   │       └── Helpers/
│   │           └── HashHelper.cs
│   └── Modules/
│       ├── Auth/
│       │   ├── Auth.Domain/
│       │   │   └── Entities/ → User
│       │   │   └── Specifications/ → UserByEmailSpecification
│       │   ├── Auth.Application/
│       │   │   ├── Services/ → IAuthService, AuthService
│       │   │   ├── DTOs/ → RegisterRequest, LoginRequest, AuthResult
│       │   │   └── Events/ → UserRegisteredEvent
│       │   ├── Auth.Infrastructure/
│       │   │   ├── AuthDbContext.cs
│       │   │   └── JwtService.cs
│       │   └── Auth.API/
│       │       ├── AuthController.cs
│       │       └── AuthModule.cs
│       ├── Tracking/
│       │   ├── Tracking.Domain/
│       │   │   ├── Entities/ → ClickEvent, ConversionEvent
│       │   │   └── Specifications/ → (10 specification classes)
│       │   ├── Tracking.Application/
│       │   │   ├── Services/ → ITrackingService, TrackingService, IClickStatsReader, ClickStatsReader
│       │   │   └── DTOs/ → ClickResult, ConversionRequest, ConversionResult, ClickStats
│       │   ├── Tracking.Infrastructure/
│       │   │   └── TrackingDbContext.cs
│       │   └── Tracking.API/
│       │       ├── TrackingController.cs
│       │       └── TrackingModule.cs (registers UserRegisteredEventHandler)
│       └── Affiliate/
│           ├── Affiliate.Domain/
│           │   ├── Entities/ → Affiliate
│           │   └── Specifications/ → AffiliateByCodeSpecification, AffiliateByIdSpecification, AffiliateByUserIdSpecification
│           ├── Affiliate.Application/
│           │   ├── Services/ → IAffiliateLookupService, AffiliateLookupService, IAffiliateDashboardService, AffiliateDashboardService
│           │   └── DTOs/ → DashboardResult, ClickStats
│           ├── Affiliate.Infrastructure/
│           │   └── AffiliateDbContext.cs
│           └── Affiliate.API/
│               ├── AffiliateController.cs
│               └── AffiliateModule.cs
└── tests/
    ├── Auth.Application.Tests/
    ├── Tracking.Application.Tests/
    ├── Affiliate.Application.Tests/
    ├── Integration.Tests/
    └── Stress.Tests/
```

---

## Phase 0: Research & Decisions

All decisions resolved:

| Decision | Choice | Rationale |
|---|---|---|
| Module isolation | Shared interfaces only | Prevents coupling; modules can evolve independently |
| Query pattern | Specification + Apply() | Named predicates, reusable, testable |
| Cross-module side effects | IEventPublisher / IEventHandler | Auth doesn't know Tracking exists |
| Click deduplication | 3-layer (cookie → app query → DB unique index) | Defense-in-depth; DB index handles race conditions |
| Attribution window | Monthly bucket in SHA-256 | Industry-standard 30-day window, no clock mocking needed in prod |
| Cache | MemoryCacheService (IMemoryCache) | Single-instance; swappable to Redis via DI |
| Test DB | SQLite in-memory | Enforces unique constraints; no external infrastructure |

---

## Phase 1: Design

### Application Startup Order

1. Serilog logger configured on the host
2. Modules collected: `AuthModule`, `TrackingModule`, `AffiliateModule`
3. In-memory cache + `IEventPublisher` registered
4. Swagger with JWT Bearer security definition
5. JWT authentication + Authorization policies + CORS (localhost:3000/5173)
6. Each module's `RegisterServices()` called in a loop
7. Controllers auto-discovered from all module assemblies
8. App built → all three DbContexts migrated (`MigrateAsync()`, MySQL only)
9. Dev data seeded (Development only)
10. `ExceptionHandlingMiddleware` + CORS → Swagger UI → Serilog request logging → Auth/Authz → Controllers

### Exception → HTTP Status Mapping

| Exception | Status |
|---|---|
| `UnauthorizedAccessException` | 401 |
| `KeyNotFoundException` | 404 |
| `ConflictException` | 409 |
| `InvalidOperationException` | 400 |
| Any other | 500 (detail hidden in Production) |

### Key Design Decisions

**Click idempotency — 3 layers:**
1. Cookie (`aff_sid`) → controller reads it, passes as existing sessionId
2. Application query → `ClickByAffiliateAndSessionSpecification` checks before insert
3. DB unique index on `(AffiliateId, SessionId)` → catches race conditions

**Attribution window:**
- `GetAttributionBucket()` = `DateTime.UtcNow.ToString("yyyy-MM")` — `protected virtual` for test overrides
- Same identity + same month → same hash → DB rejects duplicate
- Same identity + new month → different hash → new unique click

**Observer Pattern flow:**
1. `POST /api/auth/register` arrives with `aff_sid` cookie
2. `AuthController` reads cookie, passes `SessionId` into `RegisterRequest`
3. `AuthService` saves user → `eventPublisher.PublishAsync(new UserRegisteredEvent(userId, sessionId))`
4. `EventPublisher` resolves all `IEventHandler<UserRegisteredEvent>` from DI
5. `UserRegisteredEventHandler` calls `TrackingService.RecordConversionAsync(sessionId, "Registration", userId)`
6. No cookie → handler exits immediately

**Cache pattern (invalidate-on-write, lazy-load-on-read):**
- On unique click → `cache.Remove("affiliate:clickcount:{affiliateId}")`
- On dashboard load → `cache.GetOrCreateAsync("affiliate:clickcount:{affiliateId}", () => db.Count(), 5min TTL)`

---

## Phase 2: Specifications (Named Query Classes)

| Specification | Module | Filters |
|---|---|---|
| `UserByEmailSpecification` | Auth | User by email |
| `ClickByAffiliateAndSessionSpecification` | Tracking | Click by (AffiliateId + SessionId) |
| `LatestClickBySessionSpecification` | Tracking | Most recent click for SessionId (ordered desc) |
| `ConversionBySessionAndTypeSpecification` | Tracking | Conversion by (SessionId + ConversionType) |
| `ClicksByAffiliateSpecification` | Tracking | All clicks for AffiliateId |
| `RecentClicksSpecification` | Tracking | Clicks for AffiliateId since cutoff date |
| `ClickWithConversionSpecification` | Tracking | Clicks with matching ConversionEvent (EXISTS) |
| `AffiliateByCodeSpecification` | Affiliate | Affiliate by UniqueCode |
| `AffiliateByIdSpecification` | Affiliate | Affiliate by Id |
| `AffiliateByUserIdSpecification` | Affiliate | Affiliate by UserId |

---

## API Contracts

### POST /api/auth/register
- Body: `{ name, email, password }`
- 201: `{ token, expiresAt, affiliateId }`
- 409: duplicate email

### POST /api/auth/login
- Body: `{ email, password }`
- 200: `{ token, expiresAt, affiliateId }`
- 401: wrong credentials

### GET /api/tracking/click?affiliateCode=XXX
- Public endpoint
- Sets/reads `aff_sid` HttpOnly cookie (1-day lifetime)
- 200: `{ isUnique, affiliateCode, sessionId, message }`
- 404: unknown code

### POST /api/tracking/convert
- Body: `{ sessionId, conversionType, userId? }`
- 201: `{ isAttributed, affiliateCode, conversionType, message }`
- 400: invalid conversionType
- 409: duplicate conversion

### GET /api/affiliate/dashboard
- Requires `Authorization: Bearer {token}`
- 200: `{ affiliateName, uniqueCode, totalClicks, uniqueClicks, last7DayClicks, cachedClickCount }`
- 401: no/invalid token

---

## Dev Seed Data

| Email | Password | Code | Stats |
|---|---|---|---|
| alice@dev.com | DevPass123! | ALICE001 | 10 clicks, 3 conversions |
| bob@dev.com | DevPass123! | BOB00001 | 4 clicks, 0 conversions |
| carol@dev.com | DevPass123! | CAROL001 | 0 clicks |
