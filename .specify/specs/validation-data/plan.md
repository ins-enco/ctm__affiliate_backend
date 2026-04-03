---
id: validation-data-plan
version: 1.1.0
status: draft
owners:
  - tech-lead
last-reviewed: 2026-04-03
---

# Implementation Plan: Validation Data

## Technical Context

### Tech Stack
- .NET 8 / ASP.NET Core 8
- `System.ComponentModel.DataAnnotations` — built-in attributes (`[Required]`, `[EmailAddress]`, `[MinLength]`, `[MaxLength]`)
- One custom `ValidationAttribute` subclass: `[PasswordField]`
- `ApiBehaviorOptions.InvalidModelStateResponseFactory` — override to return 403 instead of default 400
- RFC 7807 ProblemDetails (existing pattern via `ExceptionHandlingMiddleware`)
- xUnit + Moq (unit tests), WebApplicationFactory (integration tests)

### Architecture Approach
- **Built-in first**: Use DataAnnotations for `[Required]`, `[EmailAddress]`, `[MinLength]`, `[MaxLength]` — no custom code needed for these.
- **Custom only where needed**: `[PasswordField]` inherits `ValidationAttribute` and is the only hand-written attribute. It lives in `CopyTradeMarketApi.Shared/Validation/` so all modules can reference it.
- **ASP.NET Core model binding handles the pipeline**: Model validation runs automatically before controller actions. We override `InvalidModelStateResponseFactory` in `Program.cs` to shape the error response as `403` + `errors` map.
- **No `IAsyncActionFilter` needed**: The built-in validation pipeline is sufficient; a custom filter would duplicate it.
- **No `DtoValidator` class needed**: `Validator.TryValidateObject()` is the engine; our only job is to reshape `ModelStateDictionary` into the correct response shape.

### Constitution Check
- [x] **P1 — Modules are islands**: `[PasswordField]` lives in `CopyTradeMarketApi.Shared`. No module references another. ✓
- [x] **P2 — Spec pattern for queries**: Not applicable — no database queries. ✓
- [x] **P3 — Domain events for side effects**: No cross-module side effects. ✓
- [x] **P4 — Secrets never in source**: No secrets involved. ✓
- [x] **P5 — Async all the way**: `InvalidModelStateResponseFactory` returns synchronously — acceptable as it performs no I/O. ✓
- [x] **P6 — Consistent error contract**: Returns RFC 7807 ProblemDetails with `errors` extension. 403 is a deliberate product decision. ✓

---

## Phase 0: Research

### Unknowns to Resolve
- **[OPEN]** Confirm `403 Forbidden` for validation failures is intentional (vs. `400`/`422`). Must be resolved before `approved` status.
- **[OPEN]** `errors` map key casing: camelCase (JSON) or original C# property name?

### Decisions Made

| Decision | Rationale | Alternatives Considered |
|---|---|---|
| Use built-in DataAnnotations for common rules | Zero new code; framework maintains it | All-custom attributes — rejected: unnecessary duplication |
| Single custom `[PasswordField]` attribute | DataAnnotations has no complexity rule; one place to maintain | FluentValidation — rejected: third-party dep, overkill for one attribute |
| Override `InvalidModelStateResponseFactory` | Least-invasive integration point; single line in `Program.cs` | Global `IAsyncActionFilter` — rejected: duplicates built-in model validation pipeline |
| `[PasswordField]` constructor params for rules | Makes attribute self-documenting at the call site; no external config needed | `IOptions<T>` config — rejected: overkill for compile-time constraints |

---

## Phase 1: Design

### Data Model

**No new database entities.**

**One new class** in `CopyTradeMarketApi.Shared/Validation/`:

```
PasswordFieldAttribute : ValidationAttribute
  Properties (constructor params with defaults):
    MinLength: int = 8
    RequireUppercase: bool = true
    RequireDigit: bool = true
    RequireSpecialChar: bool = true
  Override: IsValid(object? value, ValidationContext ctx) → ValidationResult
    Returns ValidationResult with all unmet sub-rules listed
```

---

### Interface Contracts

See [`contracts/validation-error-response.md`](contracts/validation-error-response.md).

**Response on validation failure:**
```json
{
  "status": 403,
  "title": "Validation Failed",
  "errors": {
    "email": ["The Email field is not a valid e-mail address."],
    "password": [
      "Password must be at least 8 characters",
      "Password must contain at least one uppercase letter"
    ]
  }
}
```

---

### Project Structure

**New files to create:**

```
Backend/src/Shared/CopyTradeMarketApi.Shared/
└── Validation/
    └── PasswordFieldAttribute.cs    ← custom ValidationAttribute subclass
```

**Files to modify:**

```
Backend/src/Host/CopyTradeMarketApi.Host/Program.cs
  └── Override ApiBehaviorOptions.InvalidModelStateResponseFactory → 403 + errors map

Backend/src/Modules/Auth/Auth.Application/DTOs/RegisterRequest.cs
  └── [Required][MaxLength(100)] Name, [Required][EmailAddress] Email, [Required][PasswordField] Password

Backend/src/Modules/Auth/Auth.Application/DTOs/LoginRequest.cs
  └── [Required][EmailAddress] Email, [Required] Password

Backend/src/Modules/Tracking/Tracking.Application/DTOs/ConversionRequest.cs
  └── [Required] SessionId, [Required] ConversionType
```

---

## Dependencies

| Package | Notes |
|---|---|
| `System.ComponentModel.DataAnnotations` | Already in .NET BCL — no NuGet install needed |
| `Microsoft.AspNetCore.Mvc` | Already referenced — `ApiBehaviorOptions` lives here |

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| `403` conflicts with auth semantics | Flagged in Open Questions; resolve with product owner before `approved` |
| Existing integration tests send requests without required fields | Annotate one module at a time; run tests after each to catch breakage early |
| `[PasswordField]` `IsValid` must return multiple messages | Use `ErrorMessage` with newline-separated messages or return a composite `ValidationResult` |
