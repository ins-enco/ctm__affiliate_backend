# Institution.md

---
id: institution
version: 1.2.0
status: in-review
owners:
  - tech-lead
  - product-manager
ratified: 2026-03-30
last-reviewed: 2026-04-06
---

## Purpose

CopyTradeMarket is a copy trading platform where traders publish strategies and followers copy their trades. The platform serves multiple user roles — traders, followers, operators, and affiliates — across a broader system of services.

This API (`CopyTradeMarketApi`) is the **affiliate and attribution service** within that larger platform. It is responsible for one specific domain: tracking how new users arrive on the platform through affiliate referral links, attributing their registration and deposit actions back to the correct affiliate, and providing affiliates with visibility into their performance.

It is not the trading engine, the strategy marketplace, the follower execution layer, or the operator admin system. Those exist elsewhere in the platform. This API's role is narrowly defined: identity for its own users, click and conversion tracking, and affiliate program administration — coordinated internally via domain events across three isolated modules.

---

## Scope

### Responsibility boundary

This API owns anything related to **who referred a user, how they arrived, and what actions they took after arriving**. That is its single boundary. Any capability that falls outside that boundary — regardless of how closely related it seems — belongs to another service.

New modules added to this API must be justifiable against that boundary. If a proposed module cannot answer "how does this relate to referral, attribution, or affiliate administration?" it does not belong here.

### Current modules

| Module | Owns | Responsibility |
|---|---|---|
| **Auth** | `User` entity, JWT issuance, BCrypt hashing | Identity for users within this API's boundary |
| **Tracking** | `ClickEvent`, `ConversionEvent`, session fingerprinting | Record, deduplicate, and attribute clicks and conversions |
| **Affiliate** | `Affiliate` entity, unique referral code generation | Manage affiliate accounts, referral codes, attribution data |

### Integration surface
Cross-module coordination happens via domain events (`IEventPublisher` / `IEventHandler<T>`). Integration with the rest of the CopyTradeMarket platform goes through shared abstractions in `CopyTradeMarketApi.Shared` — this is the only permitted coupling point between this API and external systems.

---

## Team composition

| Role | Responsibility in SDD | Spec-kit duty |
|---|---|---|
| Product manager | Author feature specs, own `status` transitions | Approves spec merges |
| Tech lead | Architecture decisions, own `Institution.md` | Maintains CI config, `.speckit.yml` |
| Engineering | Implement against specs, write conformance tests | Opens implementation PRs |
| QA / testing | Verify conformance test coverage, run integration suite | Reviews test stubs |
| Platform / DevOps | Docker Compose, migrations, output generation | Maintains Spec-kit version |

---

## Ways of working

### Spec-first rule
No implementation PR may be opened without a merged, `approved` spec in `.specify/specs/{feature}/spec.md`. Enforced by Spec-kit CI.

### Definition of done
- [ ] Spec exists at `.specify/specs/{feature}/spec.md`
- [ ] Implementation plan exists at `.specify/specs/{feature}/plan.md`
- [ ] All unit tests pass (`dotnet test`)
- [ ] Integration tests pass
- [ ] No compiler warnings
- [ ] No secrets in source files
- [ ] ProblemDetails returned for all error paths
- [ ] Swagger documentation reflects new endpoints

### Spec review SLA
Spec PRs must receive at least one review within **2 business days** of opening.

### Status state machine
```
draft → in-review → approved → deprecated
                  ↘ rejected
```
Skipping states requires a written exception in the PR description.

### Breaking changes
Any edit to an `approved` spec that changes behaviour, removes a field, or alters an acceptance criterion must:
1. Bump the spec `version` (minor or major)
2. Tag the PR `breaking-change`
3. Notify all teams referencing this spec

### Database changes
- New entity → new EF migration (never edit existing migrations)
- Unique constraints enforced at database level, not only in code
- Migrations tested against both MySQL (production) and SQLite (integration tests)

### API changes
- New endpoints follow `/api/{module}/{resource}` convention
- Breaking changes require a versioning discussion before implementation
- All 201 responses include a correct `Location` header or resource ID

### API version — automatic via CI (no manual bumping)

`ApiVersion` is injected automatically by the CI pipeline on every push to `main`. **No manual version bumping is required or expected.**

**How it works:**
- `appsettings.json` holds `"ApiVersion": "0.0.0-local"` — a committed fallback for local runs
- `appsettings.Development.json` holds `"ApiVersion": "0.0.0-dev"` — for local dev environment
- On every push to `main`, GitHub Actions sets `ApiVersion=<short-sha>` (7-char git SHA) as a Docker environment variable before starting the container
- .NET config precedence: environment variable wins over `appsettings.json`
- `GET /api/version` and `GET /swagger/v1/swagger.json` (`info.version`) both reflect the injected value

**FE detection:** the version changes on every deployment because every merge to `main` produces a new unique SHA.

**CI workflow:** `.github/workflows/ci.yml` — runs `dotnet test` on all PRs; injects SHA and deploys on push to `main` only.

### Security rules
- Passwords: BCrypt only — never stored plaintext or reversibly encrypted
- JWTs: HS256; claims include `sub` (userId) and `affiliateId`; validated on every protected endpoint
- No sensitive data (IP, email) logged at `Information` level — use `Debug` or omit
- Connection strings and JWT secrets never committed; provided via User Secrets (dev) or environment variables (prod)

---

## Tech stack

| Concern | Technology | Notes |
|---|---|---|
| Runtime | .NET 8 / ASP.NET Core | — |
| ORM | Entity Framework Core 8 (Pomelo MySQL) | — |
| Database | MySQL 8.0 | Production |
| Database (tests) | SQLite in-memory | Must enforce unique constraints |
| Auth | JWT Bearer HS256 | `Microsoft.AspNetCore.Authentication.JwtBearer` |
| Password hashing | BCrypt.Net-Next | — |
| Logging | Serilog | Console + rolling file |
| Caching | `IMemoryCache` via `MemoryCacheService` | Swappable to Redis |
| API docs | Swashbuckle / Swagger UI | — |
| Unit testing | xUnit + Moq | — |
| Integration testing | `WebApplicationFactory<Program>` + SQLite | — |
| Frontend (dev only) | React 19 + Vite | Mock UI — not production |
| Containerisation | Docker Compose | api + db + optional frontend |

### Architecture style
Modular monolith — one deployable ASP.NET Core 8 host, three self-contained business modules: **Auth**, **Tracking**, **Affiliate**.

Each module is fully isolated with its own Domain / Application / Infrastructure / API layers:
```
{Module}/
├── {Module}.API              # Controllers, IModule, endpoint mapping
├── {Module}.Application      # Services, DTOs, business logic
├── {Module}.Domain           # Entities, Specifications, domain rules
└── {Module}.Infrastructure   # DbContext, EF configurations, migrations
```

Cross-module communication only through shared abstractions in `CopyTradeMarketApi.Shared` — never direct project references between modules. `IAffiliateLookupService` is the canonical example.

### Domain model

| Entity | Module | Key rules |
|---|---|---|
| `User` | Auth | Email unique; password BCrypt-hashed |
| `Affiliate` | Affiliate | UniqueCode 8-char alphanumeric, globally unique; auto-generated on registration |
| `ClickEvent` | Tracking | Unique index on `(AffiliateId, SessionId)`; dedup via DB constraint |
| `ConversionEvent` | Tracking | Unique index on `(AffiliateId, SessionId, ConversionType)`; types: `Registration`, `Deposit` only |

All entities extend `BaseEntity` (`CreatedAt`, `UpdatedAt` — UTC).

---

## Code rules

### SOLID principles

**S — Single Responsibility**
Each class has one reason to change. Services handle one business capability; controllers only route and validate input; DbContexts only own persistence for their module. A service that both sends email and calculates commissions violates this — split it.

**O — Open/Closed**
Extend behaviour through new implementations, not by editing existing ones. New cache backends implement `ICacheService`; new event handlers implement `IEventHandler<T>`. The pipeline is open to extension, closed to modification.

**L — Liskov Substitution**
Any implementation must be a safe drop-in for its interface. `MemoryCacheService` and a future `RedisCacheService` must behave identically from the caller's perspective. Integration tests substitute SQLite for MySQL — this is only valid because both honour the same EF schema contract.

**I — Interface Segregation**
Interfaces are narrow and caller-shaped. `IAffiliateLookupService` exposes only what Auth needs from Affiliate — not the full `AffiliateService` surface. Never inject a fat service interface when the caller needs one method.

**D — Dependency Inversion**
High-level modules depend on abstractions, not concretions. Services are injected as interfaces via primary constructors (`public class AuthService(IAffiliateLookupService affiliates, IEventPublisher events)`). No `new ConcreteService()` inside business logic. Verified by: all cross-module and infrastructure dependencies are interface-typed in constructors.

---

### Architecture principles

**P1 — Modules are islands**
Each module owns its own DbContext, entities, and services. No module directly references another module's assembly. Cross-module needs go through `Shared` abstractions.
Verified by: no inter-module project references in `.csproj`; cross-module calls only through injected interfaces.

### Unit Tests (`*.Application.Tests`)
- **Framework**: xUnit + Moq
- **Database**: `UseInMemoryDatabase` with a unique `Guid` per test — never shared state.
- **Mocking**: Only mock interfaces crossing module boundaries (`IAffiliateLookupService`, `IEventPublisher`). Never mock EF DbContext — use in-memory instead.
- **Test naming**: `{Method}_{Condition}_{ExpectedOutcome}` (e.g., `Register_WithDuplicateEmail_ThrowsConflictException`)
- **Structure**: Arrange / Act / Assert — one assertion focus per `[Fact]`.
- **InMemory limitation**: EF Core InMemory does not enforce unique indexes. Never assert row counts or DB state that depends on uniqueness constraints — those are false positives in unit tests and contradictions in integration tests. Assert deterministic behavior via return values instead (e.g., compare `result1.SessionId == result2.SessionId`, not `db.ClickEvents.Count() == 2`). Duplicate-insert paths must be covered in `Integration.Tests` where SQLite enforces the schema.

**P3 — Domain events for side effects**
When an action in one module triggers a side effect in another, publish a domain event via `IEventPublisher.PublishAsync()`. No direct service-to-service calls across module boundaries.
Verified by: `IEventPublisher` used for all cross-module side effects. Example: `UserRegisteredEvent` (Auth) → `UserRegisteredEventHandler` (Tracking).

**P4 — Secrets never in source**
`appsettings.json` contains only `"SET_VIA_USER_SECRETS_OR_ENV"` placeholders. Secrets provided via User Secrets (dev) or environment variables (prod/Docker).

**P5 — Async all the way**
Every I/O operation is async. No `.Result` or `.Wait()` on async calls. All service methods return `Task<T>`; all EF calls use `*Async()` variants.

**P6 — Consistent error contract**
All errors surface as RFC 7807 ProblemDetails via `ExceptionHandlingMiddleware`. Services throw typed exceptions: `ConflictException`, `KeyNotFoundException`, `UnauthorizedAccessException`, `InvalidOperationException`. No service method returns `null` or a boolean to signal failure.

### C# conventions
- **Records for DTOs** — `record AuthResult(string Token, DateTime ExpiresAt, int AffiliateId)` — immutable, value semantics
- **Primary constructors** — `public class AuthService(AuthDbContext db, IJwtService jwt) { }` — no manual field assignments
- **Global usings** — common namespaces declared once in `GlobalUsings.cs` per assembly
- **Nullable reference types** — enabled project-wide; all nullable members marked `?`; no `!` suppression without justification

### Naming conventions

| Construct | Convention |
|---|---|
| Services | `{Feature}Service` / `I{Feature}Service` |
| Specifications | `{Entity}{Criteria}Specification` |
| Controllers | `{Module}Controller` |
| DbContexts | `{Module}DbContext` |
| DTOs | Named records with `Request` / `Result` suffix |
| Domain events | `{Subject}{Verb}Event` e.g. `UserRegisteredEvent` |

### Session fingerprinting rule
- Session ID: `SHA256(IPAddress + UserAgent + AffiliateCode + "yyyy-MM")`
- Monthly bucket produced by `protected virtual GetAttributionBucket()` — allows test overrides without mocking system time globally
- Cookie name: `aff_sid`, lifetime: 1 day (configurable)

### Testing standards

**Unit tests** (`*.Application.Tests`) — xUnit + Moq
- Database: `UseInMemoryDatabase` with a unique `Guid` per test — never shared state
- Only mock interfaces crossing module boundaries (`IAffiliateLookupService`, `IEventPublisher`) — never mock EF DbContext
- Naming: `{Method}_{Condition}_{ExpectedOutcome}` e.g. `Register_WithDuplicateEmail_ThrowsConflictException`
- Structure: Arrange / Act / Assert — one assertion focus per `[Fact]`

**Integration tests** (`Integration.Tests`) — xUnit + `WebApplicationFactory<Program>`
- Database: SQLite in-memory replacing all 3 MySQL contexts; must enforce unique constraints
- `ConfigureTestServices()` replaces DbContexts, JWT settings, and Bearer token validation
- `EnsureCreated()` applies full EF model including unique indexes
- Coverage: happy path + all error cases (duplicate email, invalid credentials, duplicate session, invalid conversion type)

**Stress tests** (`Stress.Tests`)
- Use `MySqlConnector` directly against a real database — not the application layer
- Validate uniqueness constraints hold under concurrent load

---

## Spec conventions

All specs live under `.specify/specs/{feature}/`. Required files per feature:

```
.specify/specs/{feature}/
├── spec.md    # What and why — acceptance criteria, domain rules
└── plan.md    # How — implementation steps, migration notes
```

Required frontmatter in every spec (enforced by Spec-kit CI):

```yaml
---
id:            # unique slug, kebab-case
version:       # semver e.g. 1.0.0
status:        # draft | in-review | approved | deprecated | rejected
owners:        # list of roles or GitHub handles
last-reviewed: # ISO date
---
```

Spec-kit CI fails on: missing required fields, empty acceptance criteria, invalid `status` transitions, or references to non-existent spec IDs.

---

## Onboarding checklist

- [ ] Read this `Institution.md` fully
- [ ] Read the Spec-kit README in the repository
- [ ] Review 2 merged spec PRs in `.specify/specs/` to understand the format
- [ ] Run the project locally via Docker Compose (`docker compose up`)
- [ ] Run the full test suite (`dotnet test`) — all tests must pass before first commit
- [ ] Pair with a teammate on your first spec authoring
- [ ] Open your first spec PR (a single acceptance criterion is enough)

---

## Decision log

| Date | Decision | Rationale | Decided by |
|---|---|---|---|
| 2026-03-30 | Modular monolith over microservices | Simpler deployment; module boundaries enforced by convention not network | Tech lead |
| 2026-03-30 | Specification pattern for all queries | Keeps query logic named, reusable, and testable | Tech lead |
| 2026-03-30 | Domain events for cross-module side effects | Decouples modules; Auth has no knowledge of Tracking | Tech lead |
| 2026-03-30 | SQLite for integration tests, MySQL for prod | Fast CI; unique constraint parity sufficient for test goals | Tech lead |
| 2026-03-30 | RFC 7807 ProblemDetails for all errors | Consistent API surface; typed exceptions make intent clear | Tech lead |
| 2026-04-03 | `ApiVersion` in `appsettings.json` as single source of truth | Propagates to both `GET /api/version` and Swagger `info.version`; no code change needed to bump | Tech lead |