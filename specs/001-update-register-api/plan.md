# Implementation Plan: Update User Registration — Extended Profile Fields

**Branch**: `001-update-register-api` | **Date**: 2026-04-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-update-register-api/spec.md`

---

## Summary

Extend the `POST /api/auth/register` endpoint and `users` database table to capture a richer
user profile: split `Name` into `FirstName` + `LastName`, add `PhoneNumber` and `Language`,
group profile fields under a `UserInformation` nested DTO, and enforce `ConfirmPassword`
validation server-side. All changes are confined to the Auth module and the Shared validation
library. The Affiliate module is untouched.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8
**Primary Dependencies**: ASP.NET Core 8, Entity Framework Core 8 (Pomelo MySQL), BCrypt.Net-Next, xUnit + Moq
**Storage**: MySQL 8.0 (production), SQLite in-memory (integration tests)
**Testing**: xUnit + `WebApplicationFactory<Program>`
**Target Platform**: Linux Docker container (ASP.NET Core 8 host)
**Project Type**: Modular monolith web service — Auth module
**Performance Goals**: Standard web API — no change from baseline
**Constraints**: `ConfirmPassword` must be validated before any DB write; no new external dependencies
**Scale/Scope**: Single `users` table migration; one new EF migration; no cross-module schema changes

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Status |
|---|---|---|
| P1 — Modules are islands | All changes within Auth module; Affiliate interface call site changes only (pass concatenated name) | ✅ PASS |
| S — Single Responsibility | `ConfirmPassword` validated at DTO layer (IValidatableObject), not in AuthService | ✅ PASS |
| O — Open/Closed | New validation attributes extend existing pattern without modifying existing attributes | ✅ PASS |
| P3 — Domain events | `UserRegisteredEvent` signature unchanged; no new cross-module events needed | ✅ PASS |
| P4 — Secrets never in source | No secrets introduced | ✅ PASS |
| P5 — Async all the way | All new EF calls use `*Async()` variants | ✅ PASS |
| P6 — Consistent error contract | Validation failures produce `400 ValidationProblemDetails`; conflict stays `409` | ✅ PASS |
| Database changes | New migration required — additive only, no existing migration edits | ✅ PASS |
| API changes | Breaking change (new required fields) on a feature branch — acceptable | ✅ PASS |

*Post-design re-check*: Design confirmed. No violations requiring justification.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-update-register-api/
├── spec.md                          ✅ done
├── research.md                      ✅ done
├── data-model.md                    ✅ done
├── plan.md                          ← this file
├── checklists/requirements.md       ✅ done
├── contracts/
│   └── register-endpoint.md        ✅ done
└── tasks.md                         ← /speckit.tasks output
```

### Source Code (affected files)

```text
Backend/src/
├── CopyTradeMarketApi.Shared/
│   └── Validation/
│       ├── PhoneFieldAttribute.cs          NEW
│       └── LanguageFieldAttribute.cs       NEW
│
└── Modules/Auth/
    ├── Auth.Domain/
    │   └── Entities/
    │       └── User.cs                     MODIFY  (add 4 fields)
    │
    ├── Auth.Application/
    │   ├── DTOs/
    │   │   ├── UserInformationDto.cs       NEW
    │   │   └── RegisterRequest.cs          MODIFY  (restructure + IValidatableObject)
    │   └── Services/
    │       └── AuthService.cs              MODIFY  (map new fields; concatenate name for affiliate)
    │
    └── Auth.Infrastructure/
        ├── Persistence/
        │   ├── Configurations/
        │   │   └── UserConfiguration.cs    MODIFY  (add 4 column configs)
        │   └── Migrations/
        │       └── <timestamp>_AddUserProfileFields.cs   NEW (EF migration)
        └── (snapshot auto-updated by EF tooling)

Backend/tests/
├── Auth.Application.Tests/
│   └── Services/
│       └── AuthServiceTests.cs            MODIFY  (update existing + add new test cases)
└── Integration.Tests/
    └── Auth/
        └── RegisterTests.cs               MODIFY  (update payloads; add new validation cases)
```

---

## Complexity Tracking

No constitution violations. No complexity justification required.

---

## Implementation Phases

### Phase 0: Shared Validation Attributes

Add two new custom attributes to `CopyTradeMarketApi.Shared.Validation` following the exact
pattern of the existing `PasswordFieldAttribute` and `StrictEmailFieldAttribute`.

- `PhoneFieldAttribute.cs` — regex `^\+?[1-9]\d{6,14}$`
- `LanguageFieldAttribute.cs` — regex `^[a-z]{2}(-[A-Z]{2})?$`

---

### Phase 1: Domain — User Entity

Extend `User.cs` with four new required string properties:
`FirstName`, `LastName`, `PhoneNumber`, `Language`.

---

### Phase 2: Infrastructure — EF Configuration + Migration

1. Update `UserConfiguration.cs` — add four property configurations:
   - `FirstName`: `varchar(50)`, required
   - `LastName`: `varchar(50)`, required
   - `PhoneNumber`: `varchar(20)`, required
   - `Language`: `varchar(10)`, required

2. Generate migration `AddUserProfileFields` via `dotnet ef migrations add`.
   Columns added with `defaultValue: ""` for existing rows.
   Verified against both MySQL and SQLite.

---

### Phase 3: Application — DTOs

1. Create `UserInformationDto.cs` — new record with five fields + validation attributes.

2. Rewrite `RegisterRequest.cs`:
   - Replace flat `Name` + `Email` with nested `UserInformation: UserInformationDto`
   - Keep `Password` with `[PasswordField]`
   - Add `ConfirmPassword` (required)
   - Implement `IValidatableObject.Validate()` for password match check
   - Keep `SessionId` as optional (set from cookie in controller)

---

### Phase 4: Application — AuthService

Update `RegisterAsync` in `AuthService.cs`:
- Map `request.UserInformation.FirstName/LastName/PhoneNumber/Language` onto new `User` fields
- Pass `$"{firstName} {lastName}"` to `CreateAffiliateAsync` — no interface change
- Use `request.UserInformation.Email` where `request.Email` was used

---

### Phase 5: Tests

**Unit tests** (`Auth.Application.Tests`):
- Update all existing `RegisterRequest` constructors to new nested shape
- Add: `Register_WithValidRequest_CreatesUserWithAllProfileFields`
- Add: `Register_WithDuplicateEmail_ThrowsConflictException` (updated payload)

**Integration tests** (`Integration.Tests`):
- Update `RegisterTests.cs` payloads to new nested structure
- Add: invalid phone → 400
- Add: invalid language → 400
- Add: mismatched passwords → 400
- Add: missing nested required fields → 400
- Happy path: verify all new fields persisted on `User`

---

## Notes

- `ConfirmPassword` is never stored — request-time validation only.
- EF migration command (from `Backend/` root):
  `dotnet ef migrations add AddUserProfileFields --project src/Modules/Auth/Auth.Infrastructure --startup-project src/CopyTradeMarketApi`
- The auto-generated `CLAUDE.md` from `update-agent-context.ps1` contains placeholder text
  (plan.md was unfilled when it ran). The authoritative project reference remains
  `.specify/memory/constitution.md`.
