# Tasks: UC-02 — User Login

Status: COMPLETED
Origin: milestone-1 (T022–T024)

---

## Phase 1 — Application Layer

- [x] T02-1 Create DTOs: `LoginRequest` record in `Auth.Application/DTOs/`
- [x] T02-2 Implement `AuthService.LoginAsync()`:
  - Find user by email via `UserByEmailSpecification` → throw `UnauthorizedAccessException` if not found
  - Verify BCrypt hash → throw `UnauthorizedAccessException` if mismatch
  - Lookup `affiliateId` via `IAffiliateLookupService.GetAffiliateIdByUserIdAsync()`
  - Return `AuthResult` with JWT, `expiresAt`, `affiliateId`

---

## Phase 2 — API Layer

- [x] T02-3 Add `POST /api/auth/login` to `AuthController`:
  - Call `AuthService.LoginAsync()`
  - Return 200 on success

---

## Phase 3 — Tests

- [x] T02-4 Unit: `Login_WithCorrectCredentials_ReturnsAuthResult`
- [x] T02-5 Unit: `Login_WithUnknownEmail_ThrowsUnauthorizedAccessException`
- [x] T02-6 Unit: `Login_WithWrongPassword_ThrowsUnauthorizedAccessException`
- [x] T02-7 Integration: `POST /api/auth/login` → 200 + valid JWT
- [x] T02-8 Integration: `POST /api/auth/login` wrong email → 401
- [x] T02-9 Integration: `POST /api/auth/login` wrong password → 401

---

## Dependencies

- UC-01 tasks must be complete (User entity, AuthDbContext, JwtService, UserByEmailSpecification)
- `IAffiliateLookupService.GetAffiliateIdByUserIdAsync()` must be implemented
