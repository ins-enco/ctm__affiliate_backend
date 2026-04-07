# Research: Email Verification and Mail Service

**Branch**: `feature/002-email-verification-service` | **Date**: 2026-04-07

---

## Decision 1: Where to place the Mail Service

**Decision**: `IMailService`, `MailMessage`, and `IEmailTemplateProvider` interfaces go in `CopyTradeMarketApi.Shared`. Concrete implementations (`SmtpMailService`, `FileSystemTemplateProvider`, `DatabaseTemplateProvider`) go in `Auth.Infrastructure`.

**Rationale**: The constitution requires cross-module abstractions to live in Shared. Placing the interfaces there means any future module can inject `IMailService` without creating inter-module coupling. Auth.Infrastructure owns the concrete implementation for now because email verification is an Auth concern.

**Alternatives considered**:
- Dedicated `Mail` module — rejected (over-engineering; no other module currently needs email)
- Everything in Auth — rejected (would prevent future reuse; violates Open/Closed principle)

---

## Decision 2: EmailVerificationToken — separate entity vs. fields on User

**Decision**: Separate `EmailVerificationToken` entity with its own table.

**Rationale**: A user can have multiple tokens over time (initial + resends); old tokens must be queryable to invalidate them. Adding multiple nullable columns to `User` would make the entity messy and force nullable-reference suppression. A dedicated table keeps `User` clean and supports the resend-invalidation requirement naturally.

**Alternatives considered**:
- Fields on `User` (IsEmailVerified, TokenValue, TokenExpiry, TokenConsumed) — rejected (single-token assumption, forces nullable columns, awkward for invalidation)

---

## Decision 3: How email dispatch is triggered (observation/event pattern)

**Decision**: Add `EmailVerificationEventHandler : IEventHandler<UserRegisteredEvent>` in `Auth.Application`. The existing `UserRegisteredEvent` (already published by `AuthService.RegisterAsync`) carries the `UserId` needed to generate and send the token.

**Rationale**: The event system (`IEventPublisher` / `IEventHandler<T>`) is already the established pattern for cross-concern side effects. No new event type is needed — `UserRegisteredEvent` already fires at the right moment. The handler runs in the same request scope but does not block the registration response because the controller returns before handler side effects are visible to the user.

**Alternatives considered**:
- Background queue / hosted service — rejected (adds complexity; not needed for v1 volume)
- Direct call from `AuthService` — rejected (violates Single Responsibility; couples Auth to mail)
- New `EmailVerificationRequestedEvent` — rejected (redundant; `UserRegisteredEvent` already covers the trigger)

---

## Decision 4: Configurable token expiry — interface abstraction

**Decision**: Define `IVerificationSettings` in `CopyTradeMarketApi.Shared`. Implement `AppSettingsVerificationSettings` in `Auth.Application`, reading from `appsettings.json` section `EmailVerification.TokenExpiryHours` (default: 24).

**Rationale**: The spec requires operator-configurability without deployment, and the discussion established that future user-self-service settings should be possible via an implementation swap. Abstracting behind an interface satisfies Open/Closed — the Auth module never changes when storage moves from config file to database.

**Alternatives considered**:
- Hardcode 24 hours — rejected (violates FR-002 and SC-007)
- New Settings module now — rejected (premature; no UI or DB-backed requirement yet)

---

## Decision 5: Template datasource resolution strategy

**Decision**: `TemplateResolver` in `Auth.Application` iterates an ordered list of `IEmailTemplateProvider` implementations (registered as `IEnumerable<IEmailTemplateProvider>` in DI). First provider to return a non-null template wins. Order is determined by DI registration sequence.

**Rationale**: Matches the Open/Closed principle — adding a new datasource type means adding a new `IEmailTemplateProvider` implementation without touching `TemplateResolver`. The fallback chain (FR-008 edge case) is a natural consequence of the ordered iteration.

**Providers for v1**:
- `FileSystemTemplateProvider` — loads from a configurable directory path
- `DatabaseTemplateProvider` — loads from Auth DB (optional; falls back gracefully if table is empty)

---

## Decision 6: Rate limiting for resend (2 min/account)

**Decision**: Store `LastVerificationEmailSentAt` on `EmailVerificationToken` (most recent record per user). Check this timestamp in `VerificationService.ResendAsync` before generating a new token.

**Rationale**: Simple, no additional infrastructure required. Querying the most recent token record gives the last-sent time. If within 2 minutes, raise `InvalidOperationException` (mapped to 429-equivalent via existing `ExceptionHandlingMiddleware`).

**Alternatives considered**:
- In-memory cache (IMemoryCache) — rejected (lost on restart; inconsistent in multi-instance deployment)
- Separate rate-limit table — rejected (over-engineering for single-field need)
