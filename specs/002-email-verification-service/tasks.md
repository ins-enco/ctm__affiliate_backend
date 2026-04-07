# Tasks: Email Verification and Mail Service

**Input**: Design documents from `/specs/002-email-verification-service/`
**Branch**: `feature/002-email-verification-service`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 / US2 / US3 — maps to user stories in spec.md

---

## Phase 1: Setup

**Purpose**: Config placeholders and exception type needed by all phases.

- [ ] T001 Add `EmailVerification`, `MailSettings`, and `EmailTemplates` sections to `Backend/src/Host/CopyTradeMarketApi.Host/appsettings.json` (placeholders only — secrets via User Secrets / env vars)
- [ ] T002 [P] Add `TooManyRequestsException` to `Backend/src/Shared/CopyTradeMarketApi.Shared/Exceptions/TooManyRequestsException.cs`
- [ ] T003 [P] Map `TooManyRequestsException → 429` in `Backend/src/Host/CopyTradeMarketApi.Host/Middleware/ExceptionHandlingMiddleware.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared interfaces, domain entity, EF config, and settings — MUST be complete before any user story.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T004 [P] Create `IMailService` interface in `Backend/src/Shared/CopyTradeMarketApi.Shared/Mail/IMailService.cs` — single method `Task SendAsync(MailMessage message)`
- [ ] T005 [P] Create `MailMessage` record in `Backend/src/Shared/CopyTradeMarketApi.Shared/Mail/MailMessage.cs` — `(string To, string Subject, string Body)`
- [ ] T006 [P] Create `EmailTemplate` record in `Backend/src/Shared/CopyTradeMarketApi.Shared/Mail/EmailTemplate.cs` — `(string Name, string Subject, string Body)`
- [ ] T007 [P] Create `IEmailTemplateProvider` interface in `Backend/src/Shared/CopyTradeMarketApi.Shared/Mail/IEmailTemplateProvider.cs` — `Task<EmailTemplate?> GetTemplateAsync(string name)`
- [ ] T008 [P] Create `IVerificationSettings` interface in `Backend/src/Shared/CopyTradeMarketApi.Shared/Verification/IVerificationSettings.cs` — property `TimeSpan TokenExpiry`
- [ ] T009 [P] Add `IsEmailVerified` bool field (default `false`) to `User` entity in `Backend/src/Modules/Auth/Auth.Domain/Entities/User.cs`
- [ ] T010 [P] Create `EmailVerificationToken` entity in `Backend/src/Modules/Auth/Auth.Domain/Entities/EmailVerificationToken.cs` extending `BaseEntity` — fields: `UserId`, `Email`, `Token`, `ExpiresAt`, `ConsumedAt?`
- [ ] T011 [P] Create `UserByVerificationTokenSpecification` in `Backend/src/Modules/Auth/Auth.Domain/Specifications/UserByVerificationTokenSpecification.cs`
- [ ] T012 Add `DbSet<EmailVerificationToken> EmailVerificationTokens` to `Backend/src/Modules/Auth/Auth.Infrastructure/Persistence/AuthDbContext.cs`
- [ ] T013 [P] Create `EmailVerificationTokenConfiguration` in `Backend/src/Modules/Auth/Auth.Infrastructure/Persistence/Configurations/EmailVerificationTokenConfiguration.cs` — unique index on `Token`, composite index on `(UserId, ConsumedAt)`, cascade delete on UserId FK
- [ ] T014 Create EF migration `AddIsEmailVerifiedToUser` — `dotnet ef migrations add AddIsEmailVerifiedToUser --project Auth.Infrastructure --startup-project CopyTradeMarketApi.Host`
- [ ] T015 Create EF migration `AddEmailVerificationToken` — `dotnet ef migrations add AddEmailVerificationToken --project Auth.Infrastructure --startup-project CopyTradeMarketApi.Host`
- [ ] T016 [P] Create `AppSettingsVerificationSettings` in `Backend/src/Modules/Auth/Auth.Application/Settings/AppSettingsVerificationSettings.cs` implementing `IVerificationSettings` — reads `EmailVerification:TokenExpiryHours` (default 24)
- [ ] T017 [P] Create `MailSettings` record in `Backend/src/Modules/Auth/Auth.Application/Settings/MailSettings.cs` — `SmtpHost`, `SmtpPort`, `SmtpUsername`, `SmtpPassword`, `FromAddress`, `FromName`, `UseSsl`

**Checkpoint**: Shared interfaces, domain entity, migrations, and settings are ready — user story phases can now begin.

---

## Phase 3: User Story 1 — Verify Email After Registration (Priority: P1) 🎯 MVP

**Goal**: After successful registration, user receives a verification email. User clicks the link, account becomes verified.

**Independent Test**: Register a new user → check that `EmailVerificationTokens` table has a record for that user → call `POST /api/auth/verify-email` with the token → confirm `Users.IsEmailVerified = true` and token `ConsumedAt` is set.

### Implementation

- [ ] T018 [P] [US1] Create `SmtpMailService` implementing `IMailService` in `Backend/src/Modules/Auth/Auth.Infrastructure/Mail/SmtpMailService.cs` — uses `MailSettings`; logs dispatch attempt, success, and failure via Serilog; catches send exceptions without rethrowing
- [ ] T019 [P] [US1] Create `FileSystemTemplateProvider` implementing `IEmailTemplateProvider` in `Backend/src/Modules/Auth/Auth.Infrastructure/Mail/FileSystemTemplateProvider.cs` — reads `{name}.subject.txt` + `{name}.body.html` from configured `EmailTemplates:FileSystemPath`; returns `null` if files not found
- [ ] T020 [P] [US1] Create `ITemplateResolver` interface in `Backend/src/Modules/Auth/Auth.Application/Templates/ITemplateResolver.cs` — `Task<EmailTemplate> ResolveAsync(string name)` (throws `InvalidOperationException` if all providers return null)
- [ ] T021 [US1] Create `TemplateResolver` implementing `ITemplateResolver` in `Backend/src/Modules/Auth/Auth.Application/Templates/TemplateResolver.cs` — iterates `IEnumerable<IEmailTemplateProvider>` in order; returns first non-null result; logs which provider resolved the template
- [ ] T022 [P] [US1] Create `VerificationEmailContext` record in `Backend/src/Modules/Auth/Auth.Application/Templates/VerificationEmailContext.cs` — `(string RecipientName, string VerificationLink, string ExpiryDescription)`; add `RenderTemplate(EmailTemplate, VerificationEmailContext)` helper that substitutes `{{RecipientName}}`, `{{VerificationLink}}`, `{{ExpiryDescription}}`
- [ ] T023 [P] [US1] Add `IVerificationService` interface to `Backend/src/Modules/Auth/Auth.Application/Services/IVerificationService.cs` — methods: `Task<string> CreateTokenAsync(int userId, string email)`, `Task VerifyAsync(string token)`
- [ ] T024 [US1] Implement `VerificationService.CreateTokenAsync` and `VerifyAsync` in `Backend/src/Modules/Auth/Auth.Application/Services/VerificationService.cs` — token: URL-safe Base64 of 64 random bytes; expiry from `IVerificationSettings.TokenExpiry`; `VerifyAsync` throws `InvalidOperationException` on expired/consumed token, `ConflictException` on already-verified user
- [ ] T025 [P] [US1] Create email template files `email-verification.subject.txt` and `email-verification.body.html` in `Backend/templates/email/` (create directory) with `{{RecipientName}}`, `{{VerificationLink}}`, `{{ExpiryDescription}}` placeholders
- [ ] T026 [US1] Create `EmailVerificationEventHandler` implementing `IEventHandler<UserRegisteredEvent>` in `Backend/src/Modules/Auth/Auth.Application/EventHandlers/EmailVerificationEventHandler.cs` — loads user by `UserId`, calls `IVerificationService.CreateTokenAsync`, resolves template, renders with `VerificationEmailContext`, calls `IMailService.SendAsync`; wraps entire dispatch in try/catch — logs failure, does NOT rethrow
- [ ] T027 [P] [US1] Add `VerifyEmailRequest` record to `Backend/src/Modules/Auth/Auth.Application/DTOs/VerifyEmailRequest.cs`
- [ ] T028 [US1] Add `VerifyEmailAsync` to `IAuthService` in `Backend/src/Modules/Auth/Auth.Application/Services/IAuthService.cs` and implement in `AuthService.cs` delegating to `IVerificationService.VerifyAsync`
- [ ] T029 [US1] Add `POST /api/auth/verify-email` endpoint to `Backend/src/Modules/Auth/Auth.API/Controllers/AuthController.cs` — accepts `VerifyEmailRequest`; returns `200 OK` on success; ProblemDetails on failure via existing middleware
- [ ] T030 [US1] Register US1 services in `Backend/src/Modules/Auth/Auth.API/AuthModule.cs` — `IVerificationSettings → AppSettingsVerificationSettings` (singleton), `IMailService → SmtpMailService` (scoped), `IEmailTemplateProvider → FileSystemTemplateProvider` (scoped), `ITemplateResolver → TemplateResolver` (scoped), `IVerificationService → VerificationService` (scoped), `IEventHandler<UserRegisteredEvent> → EmailVerificationEventHandler` (scoped, add alongside existing `UserRegisteredEventHandler`)
- [ ] T031 [US1] Write unit tests for `VerificationService` (`CreateTokenAsync`, `VerifyAsync` — valid/expired/consumed/already-verified) in `Backend/tests/Auth.Application.Tests/VerificationServiceTests.cs`
- [ ] T032 [US1] Write unit tests for `EmailVerificationEventHandler` (happy path dispatch; mail service throws — no rethrow; template not found — logs error, no rethrow) in `Backend/tests/Auth.Application.Tests/EmailVerificationEventHandlerTests.cs`
- [ ] T033 [US1] Write integration tests for `POST /api/auth/verify-email` (valid token → 200 + `IsEmailVerified = true`; expired token → 410; consumed token → 410; already verified → 409) in `Backend/tests/Integration.Tests/AuthVerificationIntegrationTests.cs`

**Checkpoint**: Register a user → verification email dispatched (or logged if SMTP not configured) → `POST /api/auth/verify-email` with the token → user is verified. US1 fully functional.

---

## Phase 4: User Story 2 — Resend Verification Email (Priority: P2)

**Goal**: An unverified user who didn't receive (or lost) the verification email can request a new one, within rate-limit constraints.

**Independent Test**: Mark a user as unverified with an existing token → call `POST /api/auth/resend-verification` → confirm old token has `ConsumedAt` set, a new token record exists, and a new email was dispatched. Call again within 2 minutes → confirm 429.

### Implementation

- [ ] T034 [P] [US2] Add `ResendVerificationRequest` record to `Backend/src/Modules/Auth/Auth.Application/DTOs/ResendVerificationRequest.cs`
- [ ] T035 [P] [US2] Add `Task ResendAsync(string email)` to `IVerificationService` in `Backend/src/Modules/Auth/Auth.Application/Services/IVerificationService.cs`
- [ ] T036 [US2] Implement `VerificationService.ResendAsync` in `Backend/src/Modules/Auth/Auth.Application/Services/VerificationService.cs` — looks up user by email (throws `KeyNotFoundException` if not found); throws `ConflictException` if already verified; checks `CreatedAt` of most recent token (throws `TooManyRequestsException` if within 2 minutes); sets `ConsumedAt = UtcNow` on all active tokens; calls `CreateTokenAsync`; dispatches email via `IMailService` with same template as US1
- [ ] T037 [US2] Add `ResendVerificationAsync` to `IAuthService` in `Backend/src/Modules/Auth/Auth.Application/Services/IAuthService.cs` and implement in `AuthService.cs` delegating to `IVerificationService.ResendAsync`
- [ ] T038 [US2] Add `POST /api/auth/resend-verification` endpoint to `Backend/src/Modules/Auth/Auth.API/Controllers/AuthController.cs` — accepts `ResendVerificationRequest`; returns `200 OK`; ProblemDetails on 404/409/429
- [ ] T039 [US2] Write unit tests for `VerificationService.ResendAsync` (success + old token invalidated; rate limited → 429; already verified → conflict; email not found → 404) in `Backend/tests/Auth.Application.Tests/VerificationServiceTests.cs`
- [ ] T040 [US2] Write integration tests for `POST /api/auth/resend-verification` (success → 200 + old token invalidated; rate limited → 429; already verified → 409; email not found → 404) in `Backend/tests/Integration.Tests/AuthVerificationIntegrationTests.cs`

**Checkpoint**: US1 + US2 both independently functional. Unverified user can request resend; rate limit enforced; old tokens invalidated.

---

## Phase 5: User Story 3 — Templated Emails from Multiple Datasources (Priority: P3)

**Goal**: The mail service can load templates from a second datasource (database) in addition to the file system. If the first source doesn't have the template, it falls through to the next.

**Independent Test**: Seed an `EmailTemplates` record in the database → configure `DatabaseTemplateProvider` as the second provider → remove the file-system template file → trigger a verification email → confirm the DB template was used and rendered correctly.

### Implementation

- [ ] T041 [P] [US3] Add `DbSet<EmailTemplate> EmailTemplates` to `Backend/src/Modules/Auth/Auth.Infrastructure/Persistence/AuthDbContext.cs` — note: `EmailTemplate` here is the DB entity, distinct from the shared record; create `EmailTemplateEntity` class in `Backend/src/Modules/Auth/Auth.Domain/Entities/EmailTemplateEntity.cs` with `Name`, `SubjectPattern`, `BodyPattern`
- [ ] T042 [P] [US3] Create `EmailTemplateEntityConfiguration` in `Backend/src/Modules/Auth/Auth.Infrastructure/Persistence/Configurations/EmailTemplateEntityConfiguration.cs` — unique index on `Name`
- [ ] T043 [US3] Create EF migration `AddEmailTemplates` — `dotnet ef migrations add AddEmailTemplates --project Auth.Infrastructure --startup-project CopyTradeMarketApi.Host`
- [ ] T044 [US3] Create `DatabaseTemplateProvider` implementing `IEmailTemplateProvider` in `Backend/src/Modules/Auth/Auth.Infrastructure/Mail/DatabaseTemplateProvider.cs` — queries `EmailTemplates` by name; maps `SubjectPattern`/`BodyPattern` to `EmailTemplate` record; returns `null` if not found
- [ ] T045 [US3] Register `DatabaseTemplateProvider` as second `IEmailTemplateProvider` in `Backend/src/Modules/Auth/Auth.API/AuthModule.cs` (after `FileSystemTemplateProvider` — DI order controls priority)
- [ ] T046 [US3] Write unit tests for `TemplateResolver` (first provider returns template → used; first returns null, second returns template → second used; both return null → `InvalidOperationException`) in `Backend/tests/Auth.Application.Tests/TemplateResolverTests.cs`
- [ ] T047 [US3] Write integration test for DB-backed template (seed `EmailTemplates` row → no file-system template → `POST /api/auth/verify-email` triggers → confirm DB template rendered) in `Backend/tests/Integration.Tests/AuthVerificationIntegrationTests.cs`

**Checkpoint**: All three user stories functional. Template fallback chain works across file system and database.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T048 [P] Add `BaseUrl` config key to `appsettings.json` (used by `EmailVerificationEventHandler` to build verification link) and document in `appsettings.json` as `"BaseUrl": "SET_VIA_ENV"`
- [ ] T049 [P] Add Serilog structured log properties for all email dispatch events in `SmtpMailService` and `EmailVerificationEventHandler` — ensure no PII (email address) logged at `Information` level per constitution security rules
- [ ] T050 [P] Add XML doc comments to all public interfaces in `CopyTradeMarketApi.Shared` (`IMailService`, `IEmailTemplateProvider`, `IVerificationSettings`) for Swagger/IDE discoverability
- [ ] T051 Run full test suite `dotnet test` from `Backend/` — confirm all tests pass with no compiler warnings
- [ ] T052 Verify `POST /api/auth/verify-email` and `POST /api/auth/resend-verification` appear correctly in Swagger UI (`GET /swagger/v1/swagger.json`)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately; T002/T003 parallel
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories; T004–T008 fully parallel; T009–T011 parallel; T012 after T010; T013 after T012; T014 after T009; T015 after T010+T013; T016/T017 parallel
- **US1 (Phase 3)**: Depends on Phase 2 completion
- **US2 (Phase 4)**: Depends on Phase 3 completion (ResendAsync extends VerificationService)
- **US3 (Phase 5)**: Depends on Phase 2 + Phase 3 (extends existing provider chain)
- **Polish (Phase 6)**: Depends on all story phases

### Parallel Opportunities Within US1

```
# Run in parallel (different files, no deps):
T018 SmtpMailService
T019 FileSystemTemplateProvider
T020 ITemplateResolver interface
T022 VerificationEmailContext + render helper
T023 IVerificationService interface
T025 Email template files
T027 VerifyEmailRequest DTO

# Sequential (depends on above):
T021 TemplateResolver (needs T020, T007)
T024 VerificationService impl (needs T023, T010, T016)
T026 EmailVerificationEventHandler (needs T018, T021, T024)
T028 IAuthService + AuthService (needs T023, T024)
T029 AuthController endpoint (needs T027, T028)
T030 AuthModule registration (needs all above)
T031–T033 Tests (needs T030)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: US1 (T018–T033)
4. **STOP and VALIDATE**: Register user → token in DB → `POST /api/auth/verify-email` → user verified
5. Demo / merge to dev-qat

### Incremental Delivery

1. **Phase 1 + 2** → Foundation ready
2. **Phase 3 (US1)** → Email verification works end-to-end → Deploy/Demo (MVP)
3. **Phase 4 (US2)** → Resend works with rate limiting → Deploy/Demo
4. **Phase 5 (US3)** → DB-backed templates work alongside file system → Deploy/Demo

---

## Notes

- `[P]` = different files, safe to parallelize
- Each story phase is independently testable without the others
- SMTP credentials must be set via User Secrets (dev) or environment variables (Docker) — never committed
- `EmailVerificationEventHandler` swallows mail errors intentionally — registration must never fail due to mail service unavailability (FR-011)
- `IVerificationSettings` abstraction means a future "user-configurable settings" feature only adds a new implementation — Auth module unchanged
