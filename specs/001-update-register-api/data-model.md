# Data Model: Update User Registration — Extended Profile Fields

**Branch**: `001-update-register-api`
**Date**: 2026-04-07

---

## Entity: `User` (Auth module)

**Table**: `users`

| Column | Type | Constraints | Change |
|---|---|---|---|
| `Id` | `int` | PK, auto-increment | unchanged |
| `Email` | `varchar(255)` | required, unique index | unchanged |
| `PasswordHash` | `varchar(255)` | required | unchanged |
| `FirstName` | `varchar(50)` | required | **NEW** |
| `LastName` | `varchar(50)` | required | **NEW** |
| `PhoneNumber` | `varchar(20)` | required | **NEW** |
| `Language` | `varchar(10)` | required | **NEW** |
| `CreatedAt` | `datetime(6)` | required | unchanged |
| `UpdatedAt` | `datetime(6)` | required | unchanged |

**Migration**: `AddUserProfileFields` — adds four columns with default values for existing rows (`''` empty string — acceptable since no live data exists on this branch).

---

## DTO: `UserInformationDto` (Auth.Application)

Nested value object within `RegisterRequest`. Not persisted independently.

| Field | Validation | Notes |
|---|---|---|
| `FirstName` | `[Required]` `[MaxLength(50)]` | |
| `LastName` | `[Required]` `[MaxLength(50)]` | |
| `Email` | `[Required]` `[StrictEmailField]` | Unique in `users` table |
| `PhoneNumber` | `[Required]` `[PhoneField]` | E.164 format |
| `Language` | `[Required]` `[LanguageField]` | BCP 47 code e.g. `"en"`, `"vi"` |

---

## DTO: `RegisterRequest` (Auth.Application)

| Field | Type | Validation | Notes |
|---|---|---|---|
| `UserInformation` | `UserInformationDto` | `[Required]` | Nested group |
| `Password` | `string` | `[Required]` `[PasswordField]` | Min 8, uppercase, digit, special char |
| `ConfirmPassword` | `string` | `[Required]` | Cross-field: must equal `Password` |
| `SessionId` | `string?` | optional | Set from cookie in controller; not in request body |

**Cross-field validation** via `IValidatableObject.Validate()`:
```
if Password != ConfirmPassword → yield ValidationResult("Passwords do not match.", ["ConfirmPassword"])
```

---

## Validation Attributes (new — Shared.Validation)

### `PhoneFieldAttribute`
- Regex: `^\+?[1-9]\d{6,14}$`
- Error: `"PhoneNumber must be a valid international phone number."`

### `LanguageFieldAttribute`
- Regex: `^[a-z]{2}(-[A-Z]{2})?$`
- Error: `"Language must be a valid BCP 47 language code (e.g. 'en', 'vi', 'en-US')."`

---

## Entity: `Affiliate` (Affiliate module — unchanged)

`Affiliate.Name` continues to store a display string. `AuthService` will pass
`$"{firstName} {lastName}"` to `CreateAffiliateAsync` — no change to the Affiliate module.

---

## State / Lifecycle

No state transitions. `User` is created atomically with affiliate profile during registration.
No partial creation states.
