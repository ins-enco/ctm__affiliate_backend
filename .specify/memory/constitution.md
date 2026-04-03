# Project Constitution: CopyTradeMarket

CONSTITUTION_VERSION: 1.0.0
RATIFICATION_DATE: 2026-03-30
LAST_AMENDED_DATE: 2026-03-30

---

## PROJECT IDENTITY

**Project Name**: CopyTradeMarket API
**Purpose**: An affiliate tracking platform that lets affiliates generate unique referral links, track clicks and conversions (Registration/Deposit), and view attribution dashboards — backed by a modular ASP.NET Core 8 API and a React dev UI.

**Core Mission**: Accurately attribute conversions to affiliates using session-based click fingerprinting (SHA256 hash), with monthly attribution windows and domain event-driven cross-module coordination.

---

## ARCHITECTURE

**Style**: Modular Monolith — one deployable ASP.NET Core 8 host, three self-contained business modules.

**Modules**: Auth, Tracking, Affiliate — each is fully isolated with its own Domain/Application/Infrastructure/API layers.

**Module Layer Structure**:
```
{Module}/
├── {Module}.API              # Controllers, IModule, endpoint mapping
├── {Module}.Application      # Services, DTOs, business logic
├── {Module}.Domain           # Entities, Specifications, domain rules
└── {Module}.Infrastructure   # DbContext, EF configurations, migrations
```

**Cross-Module Communication**: Only through shared abstractions in `CopyTradeMarketApi.Shared` — never direct project references between modules. The `IAffiliateLookupService` interface is the canonical example.

**Domain Events**: Modules communicate asynchronously via `IEventPublisher` / `IEventHandler<T>`. Example: `UserRegisteredEvent` (Auth) → `UserRegisteredEventHandler` (Tracking).

---

## PRINCIPLES

### P1 — Modules Are Islands
Each module owns its own DbContext, entities, and services. No module directly references another module's assembly. Cross-module needs go through `Shared` abstractions.

**Why**: Prevents coupling. Adding or removing a module must not break others.

**Verified by**: No inter-module project references in `.csproj` files; cross-module calls only through injected interfaces.

### P2 — Specification Pattern for All Queries
All filtered database queries use `BaseSpecification<T>` + `DbSet.Apply()`. Raw LINQ predicates are not scattered across service methods.

**Why**: Keeps query logic reusable, testable, and named (e.g., `UserByEmailSpecification`).

**Verified by**: Services call `db.{Entity}.Apply(new {Name}Specification(...))`, never `db.{Entity}.Where(x => ...)` inline.

### P3 — Domain Events for Side Effects
When an action in one module triggers a side effect in another (e.g., user registration → record conversion), publish a domain event. Do not call a service from another module directly.

**Why**: Keeps modules decoupled; the Auth module doesn't know the Tracking module exists.

**Verified by**: `IEventPublisher.PublishAsync()` used for all cross-module side effects; no direct service-to-service calls across module boundaries.

### P4 — Secrets Never In Source
Connection strings and JWT secret keys are never committed. They are provided via User Secrets (development) or environment variables (production/Docker).

**Why**: Security. Leaked secrets cannot be rotated after a git push.

**Verified by**: `appsettings.json` contains only `"SET_VIA_USER_SECRETS_OR_ENV"` placeholders for sensitive values.

### P5 — Async All the Way
Every I/O operation (database, cache, event dispatch) is async. No `.Result` or `.Wait()` on async calls.

**Why**: Prevents thread pool starvation under load; consistent with ASP.NET Core's async model.

**Verified by**: All service methods return `Task<T>`, all EF calls use `*Async()` variants.

### P6 — Consistent Error Contract
All errors are surfaced as RFC 7807 ProblemDetails via `ExceptionHandlingMiddleware`. Services throw typed exceptions (`ConflictException`, `KeyNotFoundException`, `UnauthorizedAccessException`, `InvalidOperationException`) — they do not return error codes or nullable results.

**Why**: Consistent API surface for consumers; clear intent in service code.

**Verified by**: No service method returns `null` or a boolean to signal failure; all failure paths throw.

---

## CODING STANDARDS

### C# Conventions
- **Records for DTOs**: `record AuthResult(string Token, DateTime ExpiresAt, int AffiliateId)` — immutable, value semantics.
- **Primary constructors**: `public class AuthService(AuthDbContext db, IJwtService jwt) { }` — no manual field assignments.
- **Global usings**: Common namespaces declared once in `GlobalUsings.cs` per assembly — not repeated per file.
- **Nullable reference types**: Enabled project-wide. All nullable members marked with `?`. No `!` suppression without justification.
- **Naming**:
  - Services: `{Feature}Service` / `I{Feature}Service`
  - Specifications: `{Entity}{Criteria}Specification`
  - Controllers: `{Module}Controller`
  - DbContexts: `{Module}DbContext`
  - DTOs: Named records (Request/Result suffix)
  - Domain events: `{Subject}{Verb}Event` (e.g., `UserRegisteredEvent`)

### Architecture Rules
- New modules implement `IModule` — register services in `RegisterServices()`, map endpoints in `MapEndpoints()`.
- Each module has exactly one DbContext scoped to its own database.
- Auto-migration runs at startup — migrations must be idempotent.
- No `static` state outside of `HashHelper` and similar pure utilities.

### Session Fingerprinting
- Session ID: `SHA256(IPAddress + UserAgent + AffiliateCode + "yyyy-MM")`
- The monthly bucket (`"yyyy-MM"`) is produced by a `protected virtual` method (`GetAttributionBucket()`) to allow test overrides.
- Cookie name: `aff_sid`, lifetime: 1 day (configurable).

---

## TESTING STANDARDS

### Unit Tests (`*.Application.Tests`)
- **Framework**: xUnit + Moq
- **Database**: `UseInMemoryDatabase` with a unique `Guid` per test — never shared state.
- **Mocking**: Only mock interfaces crossing module boundaries (`IAffiliateLookupService`, `IEventPublisher`). Never mock EF DbContext — use in-memory instead.
- **Test naming**: `{Method}_{Condition}_{ExpectedOutcome}` (e.g., `Register_WithDuplicateEmail_ThrowsConflictException`)
- **Structure**: Arrange / Act / Assert — one assertion focus per `[Fact]`.
- **InMemory limitation**: EF Core InMemory does not enforce unique indexes. Never assert row counts or DB state that depends on uniqueness constraints — those are false positives in unit tests and contradictions in integration tests. Assert deterministic behavior via return values instead (e.g., compare `result1.SessionId == result2.SessionId`, not `db.ClickEvents.Count() == 2`). Duplicate-insert paths must be covered in `Integration.Tests` where SQLite enforces the schema.

### Integration Tests (`Integration.Tests`)
- **Framework**: xUnit + `WebApplicationFactory<Program>`
- **Database**: SQLite in-memory (replaces all 3 MySQL contexts) — must enforce unique constraints.
- **Setup**: `ConfigureTestServices()` replaces DbContexts, JWT settings, and Bearer token validation.
- **Schema**: `EnsureCreated()` applies the full EF model including unique indexes.
- **Coverage targets**: Happy path + all error cases (duplicate email, invalid credentials, duplicate session, invalid conversion type).

### Stress Tests (`Stress.Tests`)
- Use `MySqlConnector` directly against a real database — not the application layer.
- Validate uniqueness constraints hold under concurrent load.

### Test-Override Pattern
- `TrackingService.GetAttributionBucket()` is `protected virtual` specifically to allow `AttributionWindowTests` to pin the time bucket without mocking system time globally.

---

## QUALITY GATES

### Definition of Done for a Feature
- [ ] Spec exists in `.specify/specs/{feature}/spec.md`
- [ ] Implementation plan exists in `.specify/specs/{feature}/plan.md`
- [ ] All unit tests pass (`dotnet test`)
- [ ] Integration tests pass
- [ ] No compiler warnings
- [ ] No secrets in source files
- [ ] ProblemDetails returned for all error paths (no raw exceptions leaking)
- [ ] Swagger documentation reflects new endpoints

### Database Changes
- New entity → new EF migration (never edit existing migrations)
- Unique constraints enforced at database level, not only in code
- Migration tested against both MySQL (production) and SQLite (integration tests)

### API Changes
- New endpoints follow existing REST conventions (`/api/{module}/{resource}`)
- Breaking changes require versioning discussion
- All endpoints returning 201 include correct `Location` header or resource ID

### Security
- Passwords: BCrypt only, never stored plaintext or reversibly encrypted
- JWTs: HS256, claims include `sub` (userId) and `affiliateId`; validated on every protected endpoint
- No sensitive data (IP, email) logged at `Information` level; use `Debug` or omit

---

## DOMAIN MODEL SUMMARY

| Entity | Module | Key Rules |
|---|---|---|
| `User` | Auth | Email unique; password BCrypt-hashed |
| `Affiliate` | Affiliate | UniqueCode 8-char alphanumeric, globally unique; auto-generated on registration |
| `ClickEvent` | Tracking | Unique index on `(AffiliateId, SessionId)`; dedup via DB constraint |
| `ConversionEvent` | Tracking | Unique index on `(AffiliateId, SessionId, ConversionType)`; types: `Registration`, `Deposit` only |

All entities extend `BaseEntity` (`CreatedAt`, `UpdatedAt` — UTC).

---

## TECH STACK REFERENCE

| Concern | Technology |
|---|---|
| Runtime | .NET 8 / ASP.NET Core |
| ORM | Entity Framework Core 8 (Pomelo MySQL) |
| Database | MySQL 8.0 (prod) / SQLite (tests) |
| Auth | JWT Bearer HS256 (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Password hashing | BCrypt.Net-Next |
| Logging | Serilog (console + rolling file) |
| Caching | `IMemoryCache` via `MemoryCacheService` (swappable to Redis) |
| API docs | Swashbuckle / Swagger UI |
| Unit testing | xUnit + Moq |
| Integration testing | `WebApplicationFactory<Program>` + SQLite |
| Frontend (dev) | React 19 + Vite (mock UI only) |
| Containerization | Docker Compose (api + db + optional frontend) |
