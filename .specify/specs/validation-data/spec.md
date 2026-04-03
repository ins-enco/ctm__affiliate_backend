---
id: validation-data
version: 1.1.0
status: draft
owners:
  - tech-lead
last-reviewed: 2026-04-03
---

# Feature: Validation Data

## Overview

Provide a centralised, attribute-driven validation layer that automatically inspects incoming JSON request bodies across all API modules. Developers annotate DTO properties using the platform's standard attribute set — built-in .NET DataAnnotations attributes for common rules (`[Required]`, `[EmailAddress]`, `[MinLength]`, `[MaxLength]`) and a single custom attribute `[PasswordField]` for password complexity rules that DataAnnotations does not cover.

When a request fails validation, the API returns `403 Forbidden` with a structured error body listing every violated field and the corresponding rule. This keeps validation concerns out of service methods and ensures a consistent enforcement contract across Auth, Tracking, and Affiliate modules.

---

## User Stories

### US1 - Built-in Attributes Enforce Common Field Rules (P1)
**As a** developer  
**I want to** annotate request DTO properties with standard DataAnnotations attributes  
**So that** incoming JSON is validated automatically before reaching any service logic

**Acceptance Criteria:**
- [ ] `[Required]` rejects null or whitespace-only string values
- [ ] `[EmailAddress]` rejects values that do not conform to standard email format
- [ ] `[MinLength(n)]` rejects strings shorter than `n` characters
- [ ] `[MaxLength(n)]` rejects strings longer than `n` characters
- [ ] All violated rules are collected in a single pass (not fail-fast on first error)
- [ ] A request that passes all rules proceeds to the service layer without modification

---

### US2 - Failed Validation Returns 403 with Field-Level Error Detail (P1)
**As an** API consumer  
**I want to** receive a structured error response when my request fails validation  
**So that** I can identify exactly which fields are invalid and why

**Acceptance Criteria:**
- [ ] A request with one or more validation violations returns `403 Forbidden`
- [ ] The response body conforms to RFC 7807 ProblemDetails with an additional `errors` map: `{ "fieldName": ["rule violated", ...] }`
- [ ] Each entry in `errors` names the exact DTO property and describes the violated rule in plain language
- [ ] A valid request receives no `errors` field in the response

---

### US3 - Custom `[PasswordField]` Attribute Enforces Complexity Rules (P1)
**As a** developer  
**I want to** annotate a password property with `[PasswordField]`  
**So that** the platform enforces a consistent password strength policy that DataAnnotations cannot express

**Acceptance Criteria:**
- [ ] `[PasswordField]` requires: minimum 8 characters, at least one uppercase letter, at least one digit, at least one special character
- [ ] Each unmet sub-rule is reported as a separate error message for that field
- [ ] `[PasswordField]` inherits from `System.ComponentModel.DataAnnotations.ValidationAttribute` so it integrates with the standard validation pipeline
- [ ] The minimum length and character requirements are configurable as constructor parameters on the attribute

---

### US4 - Validation Attributes Applied to Existing Module DTOs (P2)
**As a** developer  
**I want to** apply the validation attributes to all existing request DTOs  
**So that** current endpoints gain validation enforcement without new business logic

**Acceptance Criteria:**
- [ ] `RegisterRequest`: `Name` annotated `[Required]`, `[MaxLength(100)]`; `Email` annotated `[Required]`, `[EmailAddress]`; `Password` annotated `[Required]`, `[PasswordField]`
- [ ] `LoginRequest`: `Email` annotated `[Required]`, `[EmailAddress]`; `Password` annotated `[Required]`
- [ ] `ConversionRequest`: `SessionId` annotated `[Required]`; `ConversionType` annotated `[Required]`
- [ ] All existing integration tests continue to pass after annotation is applied

---

## Out of Scope

- Client-side (frontend) validation — server-side only
- File upload validation (size, MIME type)
- Cross-field / conditional validation (e.g. "fieldA required only when fieldB is set")
- Custom per-endpoint validation rules beyond the standard attribute set and `[PasswordField]`
- Changing the HTTP status code from `403` to `400` or `422` (intentional product decision; see Open Questions)

---

## Success Metrics

- Zero service methods contain manual field-level null or format checks that duplicate an available attribute
- All existing integration tests pass after rollout
- A new endpoint with annotated DTOs requires no additional validation code in the service layer

---

## Open Questions

- `403 Forbidden` conventionally signals "authenticated but not authorised". Using it for validation failures deviates from RFC 7807 norms (`400 Bad Request` or `422 Unprocessable Entity`). **Confirm this is intentional** before spec reaches `approved` status.
- Should the `errors` map use camelCase property names (matching JSON serialisation) or the original property name?
