# Implementation Plan: Email Verification and Mail Service

**Branch**: `feature/002-email-verification-service` | **Date**: 2026-04-07 | **Spec**: [spec.md](spec.md)

## Summary

After a user successfully registers, the system dispatches a verification email. The user clicks a time-limited token link to verify their account. A reusable mail service loads templates from pluggable datasources (file system and database). Email dispatch is decoupled from registration via the existing domain event system (`IEventHandler<UserRegisteredEvent>`). Token expiry is operator-configurable via `appsettings.json` through an `IVerificationSettings` abstraction that supports a future database-backed implementation.

---

## Technical Context

**Language/Version**: .NET 8 / C# 12
**Primary Dependencies**: ASP.NET Core 8, Entity Framework Core 8 (Pomelo MySQL), Serilog, xUnit + Moq
**Storage**: MySQL 8.0 (production), SQLite in-memory (integration tests)
**Testing**: xUnit + Moq (unit), `WebApplicationFactory<Program>` + SQLite (integration)
**Target Platform**: Linux server (Docker)
**Project Type**: Modular monolith web service
**Performance Goals**: Verification email dispatched within 60 seconds of registration (SC-001)
**Constraints**: Email dispatch must not block registration response; mail service failure must not fail registration
**Scale/Scope**: Consistent with existing Auth module scale

---

## Constitution Check

| Gate | Status | Notes |
|---|---|---|
| New module justifiable against API boundary | PASS | No new module — additions to Auth and Shared only |
| Cross-module calls via Shared abstractions only | PASS | `IMailService`, `IEmailTemplateProvider`, `IVerificationSettings` in Shared |
| No inter-module project references | PASS | Auth.Infrastructure → Shared only |
| SOLID — Single Responsibility | PASS | `VerificationService` owns token logic; `TemplateResolver` owns template loading; `SmtpMailService` owns dispatch |
| SOLID — Open/Closed | PASS | New template datasources implement `IEmailTemplateProvider`; no existing code changes |
| SOLID — Dependency Inversion | PASS | All consumers depend on interfaces, not concretions |
| Async all the way (P5) | PASS | All I/O async; no `.Result` / `.Wait()` |
| Secrets never in source (P4) | PASS | SMTP credentials, from-address via User Secrets / env vars |
| ProblemDetails for all errors (P6) | PASS | Reuse existing `ExceptionHandlingMiddleware` with typed exceptions |
| Domain events for cross-module side effects (P3) | PASS | `IEventHandler<UserRegisteredEvent>` triggers email dispatch |
| EF migration per new entity | PASS | 3 migrations required (see data-model.md) |

---

## Project Structure

### Documentation (this feature)

```text
specs/002-email-verification-service/
├── plan.md              ← this file
├── research.md          ← Phase 0
├── data-model.md        ← Phase 1
├── contracts/
│   └── auth-verification-endpoints.md
└── tasks.md             ← /speckit.tasks output (not yet)
```

### Source Code Changes

```text
Backend/src/
├── Shared/CopyTradeMarketApi.Shared/
│   ├── Mail/
│   │   ├── IMailService.cs                    [NEW] async send interface
│   │   ├── MailMessage.cs                     [NEW] To/Subject/Body record
│   │   └── IEmailTemplateProvider.cs          [NEW] GetTemplateAsync(name)
│   └── Verification/
│       └── IVerificationSettings.cs           [NEW] TokenExpiry property
│
├── Modules/Auth/
│   ├── Auth.Domain/
│   │   └── Entities/
│   │       └── EmailVerificationToken.cs      [NEW] entity
│   │
│   ├── Auth.Application/
│   │   ├── Services/
│   │   │   ├── IVerificationService.cs        [NEW]
│   │   │   └── VerificationService.cs         [NEW] token gen/validate/resend
│   │   ├── EventHandlers/
│   │   │   └── EmailVerificationEventHandler.cs [NEW] IEventHandler<UserRegisteredEvent>
│   │   ├── Settings/
│   │   │   ├── AppSettingsVerificationSettings.cs [NEW] reads TokenExpiryHours
│   │   │   └── MailSettings.cs                [NEW] SMTP config record
│   │   ├── Templates/
│   │   │   └── TemplateResolver.cs            [NEW] ordered IEmailTemplateProvider chain
│   │   ├── DTOs/
│   │   │   ├── VerifyEmailRequest.cs          [NEW]
│   │   │   └── ResendVerificationRequest.cs   [NEW]
│   │   └── IAuthService.cs                    [MODIFY] add VerifyEmailAsync, ResendVerificationAsync
│   │
│   ├── Auth.Infrastructure/
│   │   ├── Mail/
│   │   │   ├── SmtpMailService.cs             [NEW] IMailService implementation
│   │   │   ├── FileSystemTemplateProvider.cs  [NEW] IEmailTemplateProvider (file-based)
│   │   │   └── DatabaseTemplateProvider.cs    [NEW] IEmailTemplateProvider (DB-based)
│   │   └── Persistence/
│   │       ├── AuthDbContext.cs               [MODIFY] add EmailVerificationTokens, EmailTemplates sets
│   │       ├── Configurations/
│   │       │   ├── EmailVerificationTokenConfiguration.cs [NEW]
│   │       │   └── EmailTemplateConfiguration.cs          [NEW]
│   │       └── Migrations/
│   │           ├── *_AddIsEmailVerifiedToUser.cs          [NEW]
│   │           ├── *_AddEmailVerificationToken.cs         [NEW]
│   │           └── *_AddEmailTemplates.cs                 [NEW]
│   │
│   └── Auth.API/
│       └── Controllers/
│           └── AuthController.cs              [MODIFY] add verify + resend endpoints
│
└── tests/
    ├── Auth.Application.Tests/
    │   ├── VerificationServiceTests.cs        [NEW]
    │   └── EmailVerificationEventHandlerTests.cs [NEW]
    └── Integration.Tests/
        └── AuthVerificationIntegrationTests.cs [NEW]
```

---

## Implementation Phases

### Phase 1 — Shared Abstractions

**Goal**: Define all interfaces in `CopyTradeMarketApi.Shared`. No implementation yet.

1. Create `IMailService` — single method: `Task SendAsync(MailMessage message)`
2. Create `MailMessage` record — `(string To, string Subject, string Body)`
3. Create `IEmailTemplateProvider` — single method: `Task<EmailTemplate?> GetTemplateAsync(string name)`
4. Create `EmailTemplate` record — `(string Name, string Subject, string Body)`
5. Create `IVerificationSettings` — single property: `TimeSpan TokenExpiry`

---

### Phase 2 — Domain Entity

**Goal**: Add `EmailVerificationToken` entity and extend `User`.

1. Add `IsEmailVerified` to `User` entity (default `false`)
2. Create `EmailVerificationToken` entity extending `BaseEntity`:
   - `UserId`, `Email`, `Token` (unique), `ExpiresAt`, `ConsumedAt?`
3. Add EF configuration with unique index on `Token`, composite index on `(UserId, ConsumedAt)`
4. Add `UserByVerificationTokenSpecification`

---

### Phase 3 — Settings and Configuration

**Goal**: Bind config sections; register typed options.

1. Create `AppSettingsVerificationSettings : IVerificationSettings` — reads `EmailVerification:TokenExpiryHours`
2. Create `MailSettings` record — binds `MailSettings` config section (host, port, credentials, from)
3. Add config placeholders to `appsettings.json` (`EmailVerification`, `MailSettings`, `EmailTemplates`)
4. Secrets (`SmtpPassword`, `FromAddress`) documented in README as User Secrets keys

---

### Phase 4 — Infrastructure Implementations

**Goal**: Concrete mail service and template providers in `Auth.Infrastructure`.

1. `SmtpMailService : IMailService` — sends via SMTP using `MailSettings`; logs dispatch attempt and outcome (FR-012)
2. `FileSystemTemplateProvider : IEmailTemplateProvider`:
   - Reads files from `EmailTemplates:FileSystemPath` directory
   - File naming convention: `{template-name}.subject.txt` + `{template-name}.body.html`
   - Returns `null` if file not found (allows fallback)
3. `DatabaseTemplateProvider : IEmailTemplateProvider`:
   - Queries `EmailTemplates` table by name
   - Returns `null` if no record (allows fallback)
4. `TemplateResolver : ITemplateResolver` in `Auth.Application`:
   - Iterates `IEnumerable<IEmailTemplateProvider>` in order
   - Returns first non-null result; throws `InvalidOperationException` if all return null (FR-009 protection)

---

### Phase 5 — Verification Service

**Goal**: Token generation, validation, and resend logic in `Auth.Application`.

1. `IVerificationService` interface:
   - `Task<string> CreateTokenAsync(int userId, string email)` — generates token, persists `EmailVerificationToken`
   - `Task VerifyAsync(string token)` — validates, marks consumed, sets `User.IsEmailVerified = true`
   - `Task ResendAsync(string email)` — rate-limit check, invalidates old tokens, creates new token, returns token for dispatch
2. `VerificationService` implementation:
   - Token generation: `Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))` URL-safe encoded
   - Expiry: `DateTime.UtcNow + IVerificationSettings.TokenExpiry`
   - Rate limit: check `CreatedAt` of most recent token; throw `TooManyRequestsException` if within 2 minutes
   - Resend: set `ConsumedAt = UtcNow` on all active tokens before creating new one

**Exception mapping** (handled by existing `ExceptionHandlingMiddleware`):

| Exception | HTTP status |
|---|---|
| `ConflictException` | 409 |
| `KeyNotFoundException` | 404 |
| `InvalidOperationException` (expired/consumed token) | 410 |
| `TooManyRequestsException` (new) | 429 |

> **Note**: Add `TooManyRequestsException` to the exception hierarchy in Shared.

---

### Phase 6 — Event Handler

**Goal**: Wire email dispatch to `UserRegisteredEvent` without coupling Auth to mail sending.

1. `EmailVerificationEventHandler : IEventHandler<UserRegisteredEvent>`:
   - Calls `IVerificationService.CreateTokenAsync(event.UserId, userEmail)`
   - Looks up user email from `AuthDbContext` by `UserId`
   - Loads template via `ITemplateResolver`
   - Renders template with `VerificationEmailContext` (substitutes `{{RecipientName}}`, `{{VerificationLink}}`, `{{ExpiryDescription}}`)
   - Calls `IMailService.SendAsync(...)` — wrapped in try/catch; logs failure but does NOT rethrow (FR-011: registration must not fail)
2. Verification link format: `{BaseUrl}/api/auth/verify-email?token={token}` — `BaseUrl` from config

---

### Phase 7 — API Endpoints

**Goal**: Expose `POST /api/auth/verify-email` and `POST /api/auth/resend-verification`.

1. Add `VerifyEmailRequest` and `ResendVerificationRequest` DTOs
2. Extend `AuthController` with two new action methods
3. Update `AuthModule.RegisterServices` to register:
   - `IVerificationService → VerificationService` (scoped)
   - `IVerificationSettings → AppSettingsVerificationSettings` (singleton)
   - `IMailService → SmtpMailService` (scoped)
   - `IEmailTemplateProvider → FileSystemTemplateProvider` (scoped, first in order)
   - `IEmailTemplateProvider → DatabaseTemplateProvider` (scoped, second in order)
   - `IEventHandler<UserRegisteredEvent> → EmailVerificationEventHandler` (scoped)
   - `ITemplateResolver → TemplateResolver` (scoped)
4. Add `TooManyRequestsException → 429` mapping to `ExceptionHandlingMiddleware`

---

### Phase 8 — DB Migrations

Run in order:
1. `AddIsEmailVerifiedToUser` — adds `IsEmailVerified` column (default `false`)
2. `AddEmailVerificationToken` — creates `EmailVerificationTokens` table
3. `AddEmailTemplates` — creates `EmailTemplates` table

---

### Phase 9 — Tests

**Unit tests** (`Auth.Application.Tests`):

| Test class | Coverage |
|---|---|
| `VerificationServiceTests` | `CreateTokenAsync`, `VerifyAsync` (valid/expired/consumed), `ResendAsync` (success/rate-limited/already-verified) |
| `EmailVerificationEventHandlerTests` | Happy path dispatch; mail failure does NOT throw; template not found logs error |
| `TemplateResolverTests` | First provider returns template; first returns null, second returns template; all return null throws |

**Integration tests** (`Integration.Tests`):

| Test | Scenario |
|---|---|
| `POST /api/auth/verify-email` — valid token | 200 + user.IsEmailVerified = true |
| `POST /api/auth/verify-email` — expired token | 410 |
| `POST /api/auth/verify-email` — consumed token | 410 |
| `POST /api/auth/verify-email` — already verified | 409 |
| `POST /api/auth/resend-verification` — success | 200 + old token invalidated |
| `POST /api/auth/resend-verification` — rate limited | 429 |
| `POST /api/auth/resend-verification` — already verified | 409 |
| `POST /api/auth/resend-verification` — email not found | 404 |

---

## Complexity Tracking

No constitution violations. No additional complexity justification required.
