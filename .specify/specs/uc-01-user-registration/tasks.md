# Tasks: UC-01 — User Registration

Status: COMPLETED
Origin: milestone-1 (T015–T025)

---

## Phase 1 — Domain & Infrastructure

- [x] T01-1 Create `User` entity in `Auth.Domain/Entities/User.cs` (Id, Name, Email, PasswordHash, extends BaseEntity)
- [x] T01-2 Create `UserByEmailSpecification` in `Auth.Domain/Specifications/`
- [x] T01-3 Create `AuthDbContext` (Users DbSet, unique index on Email) in `Auth.Infrastructure/`
- [x] T01-4 Add EF migration for `users` table

---

## Phase 2 — Application Layer

- [x] T01-5 Create DTOs: `RegisterRequest`, `AuthResult` records in `Auth.Application/DTOs/`
- [x] T01-6 Create `UserRegisteredEvent { UserId, SessionId? }` in `Auth.Application/Events/`
- [x] T01-7 Create `IAffiliateLookupService.CreateAffiliateAsync()` in `Shared/Abstractions/` (used during registration)
- [x] T01-8 Create `IAuthService` + `AuthService.RegisterAsync()`:
  - BCrypt-hash password
  - Save `User` via `AuthDbContext`
  - Call `IAffiliateLookupService.CreateAffiliateAsync()` → get `affiliateId`
  - Publish `UserRegisteredEvent`
  - Return `AuthResult` with JWT, `expiresAt`, `affiliateId`
  - Throw `ConflictException` on duplicate email

---

## Phase 3 — API Layer

- [x] T01-9 Create `JwtService` (HS256, `sub` = userId + `affiliateId` claim)
- [x] T01-10 Add `POST /api/auth/register` to `AuthController`:
  - Read `aff_sid` cookie, pass `SessionId` into `RegisterRequest`
  - Call `AuthService.RegisterAsync()`
  - Return 201 on success
- [x] T01-11 Register `AuthModule.RegisterServices()` — wire AuthService, JwtService, DbContext

---

## Phase 4 — Tests

- [x] T01-12 Unit: `Register_WithNewEmail_ReturnsAuthResult`
- [x] T01-13 Unit: `Register_WithDuplicateEmail_ThrowsConflictException`
- [x] T01-14 Unit: `Register_Always_HashesPassword` (BCrypt verify)
- [x] T01-15 Unit: `Register_Always_PublishesUserRegisteredEvent`
- [x] T01-16 Integration: `POST /api/auth/register` → 201 + valid JWT
- [x] T01-17 Integration: `POST /api/auth/register` duplicate → 409

---

## Dependencies

- Shared kernel (BaseEntity, IModule, ICacheService, IEventPublisher, ConflictException, HashHelper) must exist
- `IAffiliateLookupService` (Shared) + `AffiliateLookupService` (Affiliate module) must exist before T01-8
