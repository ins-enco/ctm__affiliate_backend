# Tasks: Validation Data

## Phase 1 — Core Infrastructure

### T0 — Create `StrictEmailFieldAttribute` in Shared
- **File**: `Backend/src/Shared/CopyTradeMarketApi.Shared/Validation/StrictEmailFieldAttribute.cs`
- Inherit from `System.ComponentModel.DataAnnotations.ValidationAttribute`
- Enforce regex `^[^@\s]+@[^@\s]+\.[^@\s]+$` (requires TLD)
- Skip validation for null/empty values (defer to `[Required]`)
- Error message: `"{DisplayName} must be a valid email address"`
- [x] Done

---

### T1 — Create `PasswordFieldAttribute` in Shared
- **File**: `Backend/src/Shared/CopyTradeMarketApi.Shared/Validation/PasswordFieldAttribute.cs`
- Inherit from `System.ComponentModel.DataAnnotations.ValidationAttribute`
- Constructor params: `int minLength = 8`, `bool requireUppercase = true`, `bool requireDigit = true`, `bool requireSpecialChar = true`
- Override `IsValid(object? value, ValidationContext ctx) → ValidationResult`
- Collect all unmet sub-rule messages and return them as a single composite `ValidationResult`
- Skip validation if value is null/empty (defer to `[Required]`)
- [x] Done

---

### T2 — Override `InvalidModelStateResponseFactory` in `Program.cs`
- **File**: `Backend/src/Host/CopyTradeMarketApi.Host/Program.cs`
- After `builder.Services.AddControllers(...)`, configure `ApiBehaviorOptions`:
  ```csharp
  builder.Services.Configure<ApiBehaviorOptions>(options =>
  {
      options.InvalidModelStateResponseFactory = ctx =>
      {
          var errors = ctx.ModelState
              .Where(e => e.Value?.Errors.Count > 0)
              .ToDictionary(
                  kvp => kvp.Key,
                  kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
              );
          var problem = new ProblemDetails { Status = 403, Title = "Validation Failed" };
          problem.Extensions["errors"] = errors;
          return new ObjectResult(problem) { StatusCode = 403 };
      };
  });
  ```
- [x] Done

---

## Phase 2 — DTO Annotation (US4)

### T3 — Annotate `RegisterRequest`
- **File**: `Backend/src/Modules/Auth/Auth.Application/DTOs/RegisterRequest.cs`
- `Name`: `[Required]`, `[MaxLength(100)]`
- `Email`: `[Required]`, `[StrictEmailField]` ← replaces `[EmailAddress]`
- `Password`: `[Required]`, `[PasswordField]`
- [x] Done (updated to StrictEmailField)

### T4 — Annotate `LoginRequest`
- **File**: `Backend/src/Modules/Auth/Auth.Application/DTOs/LoginRequest.cs`
- `Email`: `[Required]`, `[StrictEmailField]` ← replaces `[EmailAddress]`
- `Password`: `[Required]`
- [x] Done (updated to StrictEmailField)

### T5 — Annotate `ConversionRequest`
- **File**: `Backend/src/Modules/Tracking/Tracking.Application/DTOs/ConversionRequest.cs`
- `SessionId`: `[Required]`
- `ConversionType`: `[Required]`
- [x] Done

---

## Phase 3 — Tests

### T6a — Unit tests for `StrictEmailFieldAttribute`
- **File**: `Backend/tests/Auth.Application.Tests/Validation/StrictEmailFieldAttributeTests.cs`
- `Validate_WithValidEmail_ReturnsSuccess` (e.g. `user@example.com`)
- `Validate_WithMissingTld_ReturnsError` (e.g. `user@example`)
- `Validate_WithNoAtSign_ReturnsError`
- `Validate_WithNullValue_ReturnsSuccess` (defers to `[Required]`)
- `Validate_WithEmptyString_ReturnsSuccess` (defers to `[Required]`)
- `Validate_WithWhitespaceOnly_ReturnsSuccess` (defers to `[Required]`)
- [x] Done

### T6 — Unit tests for `PasswordFieldAttribute`
- **File**: `Backend/tests/Auth.Application.Tests/Validation/PasswordFieldAttributeTests.cs`
- `Validate_WithValidPassword_ReturnsSuccess`
- `Validate_WithShortPassword_ReturnsLengthError`
- `Validate_WithNoUppercase_ReturnsUppercaseError`
- `Validate_WithNoDigit_ReturnsDigitError`
- `Validate_WithNoSpecialChar_ReturnsSpecialCharError`
- `Validate_WithMultipleViolations_ReturnsAllErrors`
- `Validate_WithNullValue_ReturnsSuccess` (defers to `[Required]`)
- [x] Done

### T7 — Integration test: validation returns 403
- **File**: `Backend/tests/Integration.Tests/ValidationTests.cs`
- `POST /api/auth/register` with empty body → `403` + `errors` contains `name`, `email`, `password`
- `POST /api/auth/register` with invalid email → `403` + `errors.email` present
- `POST /api/auth/register` with weak password → `403` + `errors.password` lists sub-rules
- `POST /api/auth/login` with missing fields → `403`
- `POST /api/auth/register` with valid payload → `201` (no regression)
- [x] Done
