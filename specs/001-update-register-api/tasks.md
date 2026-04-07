# Tasks: Update User Registration — Extended Profile Fields

**Input**: Design documents from `specs/001-update-register-api/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/register-endpoint.md ✅

---

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in all task descriptions

## Path Conventions

- Shared validation: `Backend/src/CopyTradeMarketApi.Shared/Validation/`
- Auth domain: `Backend/src/Modules/Auth/Auth.Domain/`
- Auth application: `Backend/src/Modules/Auth/Auth.Application/`
- Auth infrastructure: `Backend/src/Modules/Auth/Auth.Infrastructure/`
- Unit tests: `Backend/tests/Auth.Application.Tests/`
- Integration tests: `Backend/tests/Integration.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: New validation attributes needed by all subsequent phases.

- [ ] T001 [P] Create `PhoneFieldAttribute.cs` in `Backend/src/CopyTradeMarketApi.Shared/Validation/PhoneFieldAttribute.cs` — regex `^\+?[1-9]\d{6,14}$`, error message "PhoneNumber must be a valid international phone number."
- [ ] T002 [P] Create `LanguageFieldAttribute.cs` in `Backend/src/CopyTradeMarketApi.Shared/Validation/LanguageFieldAttribute.cs` — regex `^[a-z]{2}(-[A-Z]{2})?$`, error message "Language must be a valid BCP 47 language code (e.g. 'en', 'vi', 'en-US')."
- [ ] T003 [P] Add unit tests for `PhoneFieldAttribute` in `Backend/tests/Auth.Application.Tests/Validation/PhoneFieldAttributeTests.cs` — Theory with valid/invalid E.164 numbers: `+84901234567` (valid), `+1234567890` (valid), `0901234567` (valid), `123` (invalid), `""` (invalid), `+0123456789` (invalid leading zero)
- [ ] T004 [P] Add unit tests for `LanguageFieldAttribute` in `Backend/tests/Auth.Application.Tests/Validation/LanguageFieldAttributeTests.cs` — Theory: `en` (valid), `vi` (valid), `en-US` (valid), `EN` (invalid), `english` (invalid), `""` (invalid)

**Checkpoint**: Run `dotnet test` — T003 and T004 must pass before moving to Phase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entity and DB schema must exist before DTOs and service can compile.

- [ ] T005 Extend `User` entity in `Backend/src/Modules/Auth/Auth.Domain/Entities/User.cs` — add four required string properties: `FirstName`, `LastName`, `PhoneNumber`, `Language` (all `= string.Empty;`)
- [ ] T006 Update `UserConfiguration.cs` in `Backend/src/Modules/Auth/Auth.Infrastructure/Persistence/Configurations/UserConfiguration.cs` — add EF property configs: `FirstName` varchar(50) required, `LastName` varchar(50) required, `PhoneNumber` varchar(20) required, `Language` varchar(10) required
- [ ] T007 Generate EF migration — run from `Backend/` root: `dotnet ef migrations add AddUserProfileFields --project src/Modules/Auth/Auth.Infrastructure --startup-project src/CopyTradeMarketApi` — verify generated migration adds four columns with `defaultValue: ""`

**Checkpoint**: `dotnet build` must succeed. Migration file must exist before user story work begins.

---

## Phase 3: User Story 1 — Register with Full Profile Information (Priority: P1) 🎯 MVP

**Goal**: A user can POST to `/api/auth/register` with the new nested payload and receive a JWT.

**Independent Test**: `POST /api/auth/register` with valid `userInformation` object, `password`, and matching `confirmPassword` → 201 with token.

### Implementation for User Story 1

- [ ] T008 [P] [US1] Create `UserInformationDto.cs` in `Backend/src/Modules/Auth/Auth.Application/DTOs/UserInformationDto.cs` — record with `[Required][MaxLength(50)] FirstName`, `[Required][MaxLength(50)] LastName`, `[Required][StrictEmailField] Email`, `[Required][PhoneField] PhoneNumber`, `[Required][LanguageField] Language`
- [ ] T009 [US1] Rewrite `RegisterRequest.cs` in `Backend/src/Modules/Auth/Auth.Application/DTOs/RegisterRequest.cs` — replace flat `Name`/`Email` with `[Required] UserInformationDto UserInformation`, keep `[Required][PasswordField] Password`, add `[Required] string ConfirmPassword`, keep `string? SessionId`, implement `IValidatableObject.Validate()` returning `ValidationResult("Passwords do not match.", new[]{"ConfirmPassword"})` when `Password != ConfirmPassword` (depends on T008)
- [ ] T010 [US1] Update `AuthService.RegisterAsync` in `Backend/src/Modules/Auth/Auth.Application/Services/AuthService.cs` — map `request.UserInformation.FirstName/LastName/PhoneNumber/Language` onto new `User` fields; use `request.UserInformation.Email` for email lookup and assignment; pass `$"{request.UserInformation.FirstName} {request.UserInformation.LastName}"` to `CreateAffiliateAsync` (depends on T005, T009)
- [ ] T011 [US1] Update existing unit tests in `Backend/tests/Auth.Application.Tests/Services/AuthServiceTests.cs` — update all `RegisterRequest` constructors to new nested shape using `UserInformation = new UserInformationDto { FirstName="Test", LastName="User", Email=..., PhoneNumber="+84901234567", Language="en" }` (depends on T009)
- [ ] T012 [US1] Add unit test `Register_WithValidRequest_CreatesUserWithAllProfileFields` in `Backend/tests/Auth.Application.Tests/Services/AuthServiceTests.cs` — assert all four new fields are persisted on the `User` entity in the in-memory DB; assert return value contains `UserId` and `Email`, NOT a token (depends on T011)

**Checkpoint**: `dotnet test Auth.Application.Tests` — all tests pass. US1 is independently functional.

---

## Phase 4: User Story 2 — Validation Enforcement on All New Fields (Priority: P2)

**Goal**: Invalid phone, invalid language, mismatched passwords, and missing fields all return 400 with field-specific errors.

**Independent Test**: Submit 4 separate invalid payloads — each returns 400 with a descriptive `errors` object identifying the specific failing field.

### Implementation for User Story 2

- [ ] T013 [P] [US2] Update integration test file `Backend/tests/Integration.Tests/Auth/RegisterTests.cs` — update all existing register payloads to new nested `userInformation` structure (depends on T009, T010)
- [ ] T014 [P] [US2] Add integration test `Register_WithInvalidPhoneNumber_Returns400` in `Backend/tests/Integration.Tests/Auth/RegisterTests.cs` — payload with `phoneNumber: "123"` → assert 400 + `errors["UserInformation.PhoneNumber"]` present (depends on T013)
- [ ] T015 [P] [US2] Add integration test `Register_WithInvalidLanguage_Returns400` in `Backend/tests/Integration.Tests/Auth/RegisterTests.cs` — payload with `language: "english"` → assert 400 + `errors["UserInformation.Language"]` present (depends on T013)
- [ ] T016 [P] [US2] Add integration test `Register_WithMismatchedPasswords_Returns400` in `Backend/tests/Integration.Tests/Auth/RegisterTests.cs` — payload with `password: "Secure@123"` and `confirmPassword: "Different@123"` → assert 400 + `errors["ConfirmPassword"]` present (depends on T013)
- [ ] T017 [P] [US2] Add integration test `Register_WithMissingFirstName_Returns400` in `Backend/tests/Integration.Tests/Auth/RegisterTests.cs` — payload with `firstName: null` → assert 400 + `errors["UserInformation.FirstName"]` present (depends on T013)

**Checkpoint**: `dotnet test Integration.Tests` — all T013–T017 pass. Each invalid input is rejected with the correct field-level error.

---

## Phase 5: User Story 3 — Existing Integrations Remain Unaffected (Priority: P3)

**Goal**: Affiliate attribution and JWT auth continue to work after the schema change.

**Independent Test**: Register with a valid `aff_sid` cookie → assert conversion event attributed; register then login → use returned JWT on a protected endpoint → assert 200.

### Implementation for User Story 3

- [ ] T018 [US3] Add integration test `Register_WithAffiliateSession_AttributesConversion` in `Backend/tests/Integration.Tests/Auth/RegisterTests.cs` — set `aff_sid` cookie before POST, assert affiliate conversion event is attributed (depends on T013)
- [ ] T019 [US3] Add integration test `Register_ThenLogin_ThenAccessProtectedEndpoint_Returns200` in `Backend/tests/Integration.Tests/Auth/RegisterTests.cs` — register (assert 201 with `userId`/`email`, no token), then POST to `/api/auth/login` with the same credentials, use returned JWT as Bearer token on a protected endpoint, assert 200 (depends on T013)

**Checkpoint**: `dotnet test Integration.Tests` — T018 and T019 pass. No regression in existing flows.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T020 [P] Verify Swagger documentation reflects new request shape — run the API locally and confirm `POST /api/auth/register` in Swagger UI shows `userInformation` nested object with all five fields
- [ ] T021 [P] Verify migration runs cleanly against MySQL — run `docker compose up` and confirm `users` table has all four new columns via `SHOW COLUMNS FROM users`
- [ ] T022 Run full test suite `dotnet test` from `Backend/` — all unit + integration tests pass with zero warnings

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately, T001–T004 all parallel
- **Phase 2 (Foundational)**: Depends on Phase 1 build passing — T005 → T006 → T007 in sequence (each depends on previous)
- **Phase 3 (US1)**: Depends on Phase 2 complete — T008 and T009 sequence, then T010, T011, T012
- **Phase 4 (US2)**: Depends on Phase 3 complete (needs working endpoint to integration test)
- **Phase 5 (US3)**: Depends on Phase 3 complete — can run in parallel with Phase 4
- **Phase 6 (Polish)**: Depends on Phases 3–5 complete

### Within Each Phase

- Phase 1: T001, T002, T003, T004 all parallel (different files)
- Phase 2: T005 → T006 → T007 sequential (each depends on previous)
- Phase 3: T008 parallel with nothing → T009 (needs T008) → T010 (needs T009) → T011 (needs T009) → T012 (needs T011)
- Phase 4: T013 first, then T014–T017 all parallel (same file, different test methods)
- Phase 5: T018–T019 parallel (depends on T013)

### Parallel Opportunities

```
Phase 1 (all parallel):
  T001 PhoneFieldAttribute
  T002 LanguageFieldAttribute
  T003 PhoneField tests
  T004 LanguageField tests

Phase 4 (after T013):
  T014 invalid phone test
  T015 invalid language test
  T016 mismatched passwords test
  T017 missing firstName test

Phase 5 (after T013):
  T018 affiliate attribution test
  T019 JWT auth regression test
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Validation attributes + tests
2. Complete Phase 2: Entity + EF config + migration
3. Complete Phase 3: DTOs + service + unit tests
4. **STOP and VALIDATE**: `POST /api/auth/register` works end-to-end with new payload
5. Deploy/demo if ready

### Incremental Delivery

1. Phase 1 + 2 + 3 → Registration works with new fields (MVP)
2. Phase 4 → Validation errors are well-formed and field-specific
3. Phase 5 → Regression confirmed — existing flows unaffected
4. Phase 6 → Swagger + MySQL verified; clean test run

---

## Notes

- [P] = different files, no shared state, safe to parallelize
- Tests in T003/T004 should be written and run BEFORE the entity/DTO work (they test pure attribute logic — no service dependency)
- `ConfirmPassword` is never stored — validation only, stripped before `User` is created
- EF migration is additive only — safe to apply against existing data
- Total tasks: **22** across 6 phases
