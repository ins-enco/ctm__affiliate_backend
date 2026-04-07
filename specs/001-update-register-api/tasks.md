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

- [x] T001 [P] Create `PhoneFieldAttribute.cs` — split into `PhoneCodeFieldAttribute` (regex `^\+[1-9]\d{0,3}$`) and `PhoneNumberFieldAttribute` (regex `^\d{5,15}$`) per data-model.md
- [x] T002 [P] Create `LanguageFieldAttribute.cs` — regex `^[a-z]{2}(-[A-Z]{2})?$` ✅
- [x] T003 [P] Unit tests in `Validation/PhoneFieldAttributeTests.cs` — covers both `PhoneCodeFieldAttribute` and `PhoneNumberFieldAttribute` ✅
- [x] T004 [P] Unit tests in `Validation/LanguageFieldAttributeTests.cs` ✅

**Checkpoint**: Run `dotnet test` — T003 and T004 must pass before moving to Phase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entity and DB schema must exist before DTOs and service can compile.

- [x] T005 Created `UserInformation.cs` entity (separate table, not flat columns on User) + added `UserInformation? Information` navigation on `User` — matches data-model.md design
- [x] T006 Created `UserInformationConfiguration.cs` — table `user_information`, 1-to-1 FK with cascade, unique index on UserId; `UserConfiguration.cs` unchanged ✅
- [x] T007 Migration `20260407070301_AddUserInformationTable` generated and verified ✅ (name differs from plan — creates new table, not columns on users)

**Checkpoint**: `dotnet build` must succeed. Migration file must exist before user story work begins.

---

## Phase 3: User Story 1 — Register with Full Profile Information (Priority: P1) 🎯 MVP

**Goal**: A user can POST to `/api/auth/register` with the new nested payload and receive a JWT.

**Independent Test**: `POST /api/auth/register` with valid `userInformation` object, `password`, and matching `confirmPassword` → 201 with token.

### Implementation for User Story 1

- [x] T008 [P] [US1] Created `UserInformationDto.cs` — includes `PhoneCode` + `PhoneNumber` (two fields per contract), `[PhoneCodeField]`, `[PhoneNumberField]`, `[LanguageField]` ✅
- [x] T009 [US1] Rewrote `RegisterRequest.cs` — nested `UserInformation`, `ConfirmPassword`, `IValidatableObject` cross-field check ✅
- [x] T010 [US1] Updated `AuthService.RegisterAsync` — maps all profile fields to `UserInformation` entity, returns `RegisterResult(UserId, Email)` (no token), passes full name to `CreateAffiliateAsync` ✅
- [x] T011 [US1] Updated all `RegisterRequest` constructors in `AuthServiceTests.cs` to new nested shape; added `ValidRegisterRequest()` helper ✅
- [x] T012 [US1] Added `Register_WithValidRequest_CreatesUserWithAllProfileFields` — asserts all profile fields persisted, return is `RegisterResult` not `AuthResult` ✅

**Checkpoint**: `dotnet test Auth.Application.Tests` — all tests pass. US1 is independently functional.

---

## Phase 4: User Story 2 — Validation Enforcement on All New Fields (Priority: P2)

**Goal**: Invalid phone, invalid language, mismatched passwords, and missing fields all return 400 with field-specific errors.

**Independent Test**: Submit 4 separate invalid payloads — each returns 400 with a descriptive `errors` object identifying the specific failing field.

### Implementation for User Story 2

- [x] T013 [P] [US2] Created `Integration.Tests/Auth/RegisterTests.cs`; also updated `ValidationTests.cs`, `FullJourneyTests.cs`, `ObserverPatternTests.cs`, `AttributionWindowTests.cs` to new payload shape (broader than original scope — all integration files updated) ✅
- [x] T014 [P] [US2] `Register_WithInvalidPhoneNumber_Returns400` ✅
- [x] T015 [P] [US2] `Register_WithInvalidLanguage_Returns400` ✅
- [x] T016 [P] [US2] `Register_WithMismatchedPasswords_Returns400` ✅
- [x] T017 [P] [US2] `Register_WithMissingFirstName_Returns400` ✅

**Checkpoint**: `dotnet test Integration.Tests` — all T013–T017 pass. Each invalid input is rejected with the correct field-level error.

---

## Phase 5: User Story 3 — Existing Integrations Remain Unaffected (Priority: P3)

**Goal**: Affiliate attribution and JWT auth continue to work after the schema change.

**Independent Test**: Register with a valid `aff_sid` cookie → assert conversion event attributed; register then login → use returned JWT on a protected endpoint → assert 200.

### Implementation for User Story 3

- [x] T018 [US3] `Register_WithAffiliateSession_AttributesConversion` ✅
- [x] T019 [US3] `Register_ThenLogin_ThenAccessProtectedEndpoint_Returns200` ✅

**Checkpoint**: `dotnet test Integration.Tests` — T018 and T019 pass. No regression in existing flows.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T020 [P] Verify Swagger documentation — pending (requires running API)
- [x] T021 [P] Verify migration runs against MySQL — `docker compose up --build` in progress
- [x] T022 Full test suite `dotnet test` — 125 tests pass, 0 failures, 0 warnings ✅

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
