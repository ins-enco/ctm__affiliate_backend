# Data Model: Validation Data

## Overview

No new database entities. This feature introduces **C# custom validation attributes** and a **configuration options record**. All constructs live in `CopyTradeMarketApi.Shared/Validation/`.

---

## Validation Attributes

All attributes inherit from `System.Attribute` and implement `IValidationAttribute` (a new marker interface defined in Shared).

### `IValidationAttribute`
```
interface IValidationAttribute
  Validate(value: object?, fieldName: string, options: ValidationContext) → IEnumerable<string>
```

---

### `RequiredFieldAttribute`
| Property | Value |
|---|---|
| Target | Property |
| AllowMultiple | false |
| Rule | Value must be non-null and, for strings, non-whitespace |
| Error message | `"{fieldName} is required"` |

---

### `EmailFieldAttribute`
| Property | Value |
|---|---|
| Target | Property |
| AllowMultiple | false |
| Pre-processing | Trim whitespace before evaluation |
| Rule | Value must match standard email format (`local@domain.tld`) |
| Error message | `"{fieldName} must be a valid email address"` |
| Notes | Empty/null triggers `RequiredField` error if `[RequiredField]` is also present; `EmailField` is skipped for null/empty |

---

### `PasswordFieldAttribute`
| Property | Value |
|---|---|
| Target | Property |
| AllowMultiple | false |
| Rules | Each sub-rule produces a separate error message |
| Sub-rules | Min length (configurable, default 8), at least 1 uppercase, at least 1 digit, at least 1 special character |
| Error messages | `"Password must be at least {n} characters"`, `"Password must contain at least one uppercase letter"`, `"Password must contain at least one digit"`, `"Password must contain at least one special character"` |
| Configuration | `PasswordValidationOptions` injected into `DtoValidator` |

---

### `NameFieldAttribute`
| Property | Value |
|---|---|
| Target | Property |
| AllowMultiple | false |
| Rules | Non-null, non-whitespace (delegates to `RequiredField` semantics); max length TBD (open question) |
| Error message | `"{fieldName} is not a valid name"` |

---

### `MinLengthFieldAttribute`
| Property | Value |
|---|---|
| Target | Property |
| Constructor | `MinLengthFieldAttribute(int minimum)` |
| Rule | String length ≥ `minimum` |
| Error message | `"{fieldName} must be at least {minimum} characters"` |

---

### `MaxLengthFieldAttribute`
| Property | Value |
|---|---|
| Target | Property |
| Constructor | `MaxLengthFieldAttribute(int maximum)` |
| Rule | String length ≤ `maximum` |
| Error message | `"{fieldName} must be no more than {maximum} characters"` |

---

## Configuration

### `PasswordValidationOptions`
```
record PasswordValidationOptions
  MinLength: int = 8
  RequireUppercase: bool = true
  RequireDigit: bool = true
  RequireSpecialChar: bool = true
```
Section key in `appsettings.json`: `"PasswordValidation"`

---

## `DtoValidator` (Shared Service)

```
class DtoValidator
  + static Validate(dto: object, options: PasswordValidationOptions) → Dictionary<string, List<string>>
```
- Uses reflection to enumerate properties of `dto`
- Caches `PropertyInfo[]` per type in a `ConcurrentDictionary<Type, PropertyInfo[]>`
- For each property with one or more `IValidationAttribute`, calls `Validate()` and collects errors under the camelCase property name
- Returns empty dictionary if no violations

---

## Annotated DTOs (after US5)

| DTO | Property | Attributes |
|---|---|---|
| `RegisterRequest` | `Name` | `[RequiredField]`, `[NameField]` |
| `RegisterRequest` | `Email` | `[RequiredField]`, `[EmailField]` |
| `RegisterRequest` | `Password` | `[RequiredField]`, `[PasswordField]` |
| `LoginRequest` | `Email` | `[RequiredField]`, `[EmailField]` |
| `LoginRequest` | `Password` | `[RequiredField]`, `[PasswordField]` |
| `ConversionRequest` | `SessionId` | `[RequiredField]` |
| `ConversionRequest` | `ConversionType` | `[RequiredField]` |
