# Data Model: Update User Registration — Extended Profile Fields

**Branch**: `001-update-register-api`
**Date**: 2026-04-07 (updated 2026-04-07 — split User / UserInformation tables)

---

## Entity: `User` (Auth module)

**Table**: `users`

| Column | Type | Constraints | Change |
|---|---|---|---|
| `Id` | `int` | PK, auto-increment | unchanged |
| `Email` | `varchar(255)` | required, unique index | unchanged |
| `PasswordHash` | `varchar(255)` | required | unchanged |
| `CreatedAt` | `datetime(6)` | required | unchanged |
| `UpdatedAt` | `datetime(6)` | required | unchanged |

`users` stores **authentication credentials only**. No personal data.

---

## Entity: `UserInformation` (Auth module)

**Table**: `user_information`

| Column | Type | Constraints | Change |
|---|---|---|---|
| `Id` | `int` | PK, auto-increment | **NEW** |
| `UserId` | `int` | FK → `users.Id`, unique index (1-to-1) | **NEW** |
| `FirstName` | `varchar(50)` | required | **NEW** |
| `LastName` | `varchar(50)` | required | **NEW** |
| `PhoneCode` | `varchar(10)` | required | **NEW** |
| `PhoneNumber` | `varchar(20)` | required | **NEW** |
| `Language` | `varchar(10)` | required | **NEW** |
| `CreatedAt` | `datetime(6)` | required | **NEW** |
| `UpdatedAt` | `datetime(6)` | required | **NEW** |

**Relationship**: one-to-one with `User` via `UserId`. Created atomically with `User`
in a single `SaveChangesAsync()` call during registration.

**Module**: Auth — lives in `AuthDbContext` alongside `User`. No cross-module dependency.

**Migration**: `AddUserInformationTable` — new table only. `users` table is unchanged.

---

## DTO: `UserInformationDto` (Auth.Application)

Nested value object within `RegisterRequest`. Maps to `UserInformation` entity at registration.

| Field | Validation | Notes |
|---|---|---|
| `FirstName` | `[Required]` `[MaxLength(50)]` | |
| `LastName` | `[Required]` `[MaxLength(50)]` | |
| `Email` | `[Required]` `[StrictEmailField]` | Stored in `users.Email` (unique) |
| `PhoneCode` | `[Required]` `[PhoneCodeField]` | Dial code e.g. `+84`, `+1` |
| `PhoneNumber` | `[Required]` `[PhoneNumberField]` | Local number e.g. `901234567` |
| `Language` | `[Required]` `[LanguageField]` | BCP 47 code e.g. `"en"`, `"vi"` |

---

## DTO: `RegisterRequest` (Auth.Application)

| Field | Type | Validation | Notes |
|---|---|---|---|
| `UserInformation` | `UserInformationDto` | `[Required]` | Nested group |
| `Password` | `string` | `[Required]` `[PasswordField]` | Min 8, uppercase, digit, special char |
| `ConfirmPassword` | `string` | `[Required]` | Cross-field: must equal `Password` |
| `SessionId` | `string?` | optional | Set from cookie in controller; not in body |

**Cross-field validation** via `IValidatableObject.Validate()`:
```
if Password != ConfirmPassword → yield ValidationResult("Passwords do not match.", ["ConfirmPassword"])
```

---

## Validation Attributes (new — Shared.Validation)

### `PhoneCodeFieldAttribute`
- Regex: `^\+[1-9]\d{0,3}$` (e.g. `+84`, `+1`, `+886`)
- Error: `"PhoneCode must be a valid country dial code (e.g. '+84', '+1')."`

### `PhoneNumberFieldAttribute`
- Regex: `^\d{5,15}$` (local subscriber number, digits only, no country code)
- Error: `"PhoneNumber must be a valid local phone number (digits only, no country code)."`

### `LanguageFieldAttribute`
- Regex: `^[a-z]{2}(-[A-Z]{2})?$`
- Error: `"Language must be a valid BCP 47 language code (e.g. 'en', 'vi', 'en-US')."`

---

## Entity: `Affiliate` (Affiliate module — unchanged)

`Affiliate.Name` continues to store a display string. `AuthService` will pass
`$"{firstName} {lastName}"` to `CreateAffiliateAsync` — no change to the Affiliate module.

---

## State / Lifecycle

`User` and `UserInformation` are created atomically in a single transaction during
registration. No partial creation states — if either insert fails, both are rolled back.
