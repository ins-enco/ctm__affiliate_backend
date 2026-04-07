# Research: Update User Registration — Extended Profile Fields

**Branch**: `001-update-register-api`
**Date**: 2026-04-07

---

## Decision 1: PhoneNumber validation approach

**Decision**: Use a custom `[PhoneField]` data annotation attribute following the same pattern as `[PasswordField]` and `[StrictEmailField]` in `CopyTradeMarketApi.Shared.Validation`. Validate E.164 format (`^\+?[1-9]\d{6,14}$`) which covers international numbers with optional leading `+`.

**Rationale**: Consistent with existing validation pattern. No external library dependency needed. E.164 is the standard used by most international telecom systems and accepted by frontend libraries.

**Alternatives considered**:
- libphonenumber-csharp: Full carrier/region validation but 2MB+ dependency for simple format check — overkill.
- FluentValidation: Not in use in this project; introducing it for one field violates SOLID O and adds stack inconsistency.

---

## Decision 2: Language code validation

**Decision**: Accept BCP 47 / ISO 639-1 two-letter language codes (e.g. `"en"`, `"vi"`, `"zh"`). Validate with a simple regex `^[a-z]{2}(-[A-Z]{2})?$` covering both `"en"` and `"en-US"` forms. Store as-is — no resolution to a full locale object.

**Rationale**: Simple, no external dependency, covers the expected use case (UI language preference). The spec explicitly states that no internationalisation beyond storing the preference is in scope.

**Alternatives considered**:
- CultureInfo.GetCultureInfo() lookup: Runtime exception-based validation is not idiomatic for data annotations.
- Enum of supported languages: Too rigid — adding a language requires a code change and migration.

---

## Decision 3: ConfirmPassword — validation placement

**Decision**: Validate `Password == ConfirmPassword` in `AuthService.RegisterAsync()` using a custom `[ConfirmPassword]` cross-field comparison, implemented as a class-level `IValidatableObject` on `RegisterRequest`. This fires during ASP.NET Core model binding before the service layer is reached.

**Rationale**: The spec mandates backend enforcement (FR-004). ASP.NET Core's model validation via `IValidatableObject` on the DTO record is the right layer — it runs before the controller hands off to the service, produces a standard `400 ValidationProblemDetails` response, and requires no changes to `IAuthService`.

**Alternatives considered**:
- Validate in `AuthService`: Adds non-business-logic to the service layer; violates S (Single Responsibility).
- Custom `[Compare]` attribute: Does not work on records with `init` properties in .NET 8 (no getter/setter symmetry). `IValidatableObject` is the correct approach.

---

## Decision 4: `IAffiliateLookupService.CreateAffiliateAsync(int userId, string name)` — what to pass for `name`

**Decision**: Pass `$"{request.UserInformation.FirstName} {request.UserInformation.LastName}"` (concatenated full name) to `CreateAffiliateAsync`. The `Affiliate.Name` field is a display label; its format is internal to the Affiliate module.

**Rationale**: `IAffiliateLookupService` is a Shared abstraction — we cannot change its signature without coordinating across the Affiliate module boundary. Concatenating the name at the call site in `AuthService` is minimal, contained, and keeps the interface stable.

**Alternatives considered**:
- Change `CreateAffiliateAsync` signature to accept `(userId, firstName, lastName)`: Crosses module boundary — requires Affiliate module change and violates P1 (Modules are islands) without cross-team coordination.
- Store only `FirstName` in Affiliate.Name: Misleading — affiliate display names are expected to be full names.

---

## Decision 5: `RegisterRequest` payload structure — `UserInformation` group

**Decision**: Implement `UserInformation` as a nested record inside `RegisterRequest`:

```csharp
public record UserInformationDto
{
    [Required][MaxLength(50)]    public string FirstName { get; init; } = null!;
    [Required][MaxLength(50)]    public string LastName { get; init; } = null!;
    [Required][StrictEmailField] public string Email { get; init; } = null!;
    [Required][PhoneField]       public string PhoneNumber { get; init; } = null!;
    [Required][LanguageField]    public string Language { get; init; } = null!;
}

public record RegisterRequest : IValidatableObject
{
    [Required] public UserInformationDto UserInformation { get; init; } = null!;
    [Required][PasswordField] public string Password { get; init; } = null!;
    [Required] public string ConfirmPassword { get; init; } = null!;
    public string? SessionId { get; init; }
}
```

**Rationale**: FR-008 requires grouping. Nested record is idiomatic in this codebase (records for DTOs). ASP.NET Core model binding handles nested objects naturally. `[Required]` on the nested record ensures the group itself is present.

**Alternatives considered**:
- Flat DTO with `UserInfo_` prefixed fields: Ugly; no structural separation.
- Separate endpoint for profile: Out of scope; registration is a single atomic action.

---

## Decision 6: `User` entity — new fields

**Decision**: Add `FirstName`, `LastName`, `PhoneNumber`, `Language` directly to `User` entity (flat). All required, no nullable.

**Rationale**: The spec states "persistence layer may store fields flat on the User entity." These are first-class identity attributes. No separate profile table is needed at this stage — premature normalisation given the current module scope.

**EF Configuration additions** (in `UserConfiguration`):
- `FirstName`: `varchar(50)`, required
- `LastName`: `varchar(50)`, required
- `PhoneNumber`: `varchar(20)`, required
- `Language`: `varchar(10)`, required

**Migration**: New migration `AddUserProfileFields` — additive only (new columns with defaults for existing rows).

---

## Decision 7: Existing `Name` field in `RegisterRequest` / `Affiliate.Name`

**Decision**: Remove `Name` from `RegisterRequest` entirely. `Affiliate.Name` continues to exist unchanged — it receives the concatenated `"FirstName LastName"` string as described in Decision 4.

**Rationale**: The spec confirms `Name` was never persisted to `User`. Removing it is a non-breaking change to the `User` entity. It is a breaking change to the `RegisterRequest` API contract — acceptable as this is an in-flight feature on a dedicated branch.
