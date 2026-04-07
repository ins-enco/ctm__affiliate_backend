# Feature Specification: Update User Registration — Extended Profile Fields

**Feature Branch**: `001-update-register-api`
**Created**: 2026-04-07
**Status**: Draft
**Input**: Update register API and database to adapt new design — Language, PhoneNumber, ConfirmPassword (backend validation), split Name → FirstName + LastName, keep Email, organise into UserInformation group.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Register with Full Profile Information (Priority: P1)

A new user submits a registration form that captures their first name, last name, email,
phone number, preferred language, password, and password confirmation. The system validates
all fields — including that the two passwords match — persists the user's identity and profile
information, and confirms the account was created. The user must then call the login endpoint
separately to obtain an access token.

**Why this priority**: This is the core registration flow. Nothing else works until a user
can successfully create an account with the new field set.

**Independent Test**: Can be fully tested by submitting a valid registration payload with
all new fields and verifying the user is created and a `{ userId, email }` response is returned.

**Acceptance Scenarios**:

1. **Given** a new user with valid first name, last name, email, phone number, language,
   password, and matching confirm-password, **When** they submit the registration request,
   **Then** the system creates the account, persists all profile fields, and returns a
   `201 Created` response containing `userId` and `email` — no token.

2. **Given** a registration request where `ConfirmPassword` does not match `Password`,
   **When** the request is submitted, **Then** the system MUST reject it with a clear
   validation error — regardless of what the client submitted.

3. **Given** a registration request with an email that already exists,
   **When** submitted, **Then** the system returns a conflict error and no duplicate
   account is created.

---

### User Story 2 — Validation Enforcement on All New Fields (Priority: P2)

The system enforces input rules on every new field so that only clean, meaningful data
enters the platform. Partial or malformed registrations are rejected before any data
is persisted.

**Why this priority**: Data integrity at registration prevents downstream issues in
affiliate attribution, communication, and localisation features.

**Independent Test**: Submit individual invalid payloads (missing first name, invalid
phone format, unsupported language code, mismatched passwords) and verify each is
rejected with a descriptive error.

**Acceptance Scenarios**:

1. **Given** a request missing `FirstName` or `LastName`,
   **When** submitted, **Then** the system returns a validation error identifying
   which name field is missing.

2. **Given** a request with a `PhoneNumber` that does not conform to a recognised
   international format, **When** submitted, **Then** the system returns a validation
   error for the phone field.

3. **Given** a request with an unsupported or blank `Language` value,
   **When** submitted, **Then** the system returns a validation error specifying the
   accepted language format.

4. **Given** a request with a `Password` that does not meet complexity rules,
   **When** submitted, **Then** the system rejects it with a password-strength error.

---

### User Story 3 — Existing Integrations Remain Unaffected (Priority: P3)

Affiliate session attribution, conversion tracking, and JWT-based access continue to
work correctly after registration with the new field set. No existing downstream
behaviour is broken by the schema change.

**Why this priority**: The platform already has live affiliate and tracking flows.
Regression here would silently break revenue attribution.

**Independent Test**: Complete a registration with a valid affiliate session cookie
present and verify the conversion event is still attributed correctly.

**Acceptance Scenarios**:

1. **Given** a valid affiliate session cookie is present at registration time,
   **When** the user registers with the new payload, **Then** the conversion event
   is attributed to the correct affiliate as before.

2. **Given** a successfully registered user, **When** they call `POST /api/auth/login`
   with their credentials, **Then** a valid JWT is returned and can be used to access
   protected endpoints with correct claims.

---

### Edge Cases

- What happens when `PhoneNumber` is supplied with spaces, dashes, or country code
  prefixes? (System MUST normalise or reject consistently — not silently truncate.)
- What happens when `Language` is a valid but unsupported locale? (Reject or fall back
  to a default — decision must be explicit and documented.)
- What happens when both `Password` and `ConfirmPassword` are present but one is
  empty string? (Treated as mismatch — not as "no confirmation provided".)
- What happens when a client omits `ConfirmPassword` entirely? (Field is required;
  request MUST be rejected, not silently ignored.)

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept `FirstName` and `LastName` as separate required fields
  in the registration request, replacing the single `Name` field.

- **FR-002**: System MUST accept `PhoneCode` (country dial code, e.g. `+84`, `+1`) and
  `PhoneNumber` (local subscriber number without country code) as two separate required fields.
  Both are validated and stored independently.

- **FR-003**: System MUST accept `Language` as a required field representing the user's
  preferred language, validated as a well-formed language code (e.g. BCP 47 / ISO 639-1).

- **FR-004**: System MUST accept `ConfirmPassword` as a required field and validate
  server-side that it matches `Password` exactly. A mismatch MUST produce a validation
  error; the field MUST NOT be optional or skipped for API calls that bypass the frontend.

- **FR-005**: System MUST persist `FirstName`, `LastName`, `PhoneNumber`, and `Language`
  as part of the user's stored profile alongside the existing `Email`.

- **FR-006**: System MUST continue to enforce email uniqueness across all registrations.

- **FR-007**: System MUST continue to attribute an affiliate conversion event when a
  valid session identifier is present at registration time.

- **FR-008**: The registration request payload MUST organise user profile fields
  (`FirstName`, `LastName`, `Email`, `PhoneNumber`, `Language`) into a clearly
  named `UserInformation` group, separate from credential fields (`Password`,
  `ConfirmPassword`) and tracking fields (`SessionId`).

### Key Entities

- **User**: Represents an authenticated identity within the affiliate API boundary.
  Key attributes: unique email, first name, last name, phone number, preferred
  language, hashed credentials. Relationships: one-to-one with `Affiliate` on
  registration if a referral session is present.

- **RegisterRequest**: The inbound registration payload. Logically divided into:
  - *UserInformation* — `FirstName`, `LastName`, `Email`, `PhoneNumber`, `Language`
  - *Credentials* — `Password`, `ConfirmPassword`
  - *Tracking* — `SessionId` (optional)

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can complete registration with all new fields in a single request
  and receive a confirmation response containing their `userId` and `email`. A separate
  login request is required to obtain an access token.

- **SC-002**: 100% of registration requests where `ConfirmPassword ≠ Password` are
  rejected before any data is written to storage.

- **SC-003**: 100% of registrations with an invalid phone number or unsupported
  language code are rejected with a field-specific error message.

- **SC-004**: All existing affiliate attribution and JWT authentication acceptance
  tests continue to pass without modification after the schema change is deployed.

- **SC-005**: No existing registration data is lost or corrupted during the database
  migration that introduces the new columns.

---

## Assumptions

- `ConfirmPassword` is a request-time validation field only — it is never stored.
- `Language` is stored as a locale code string (e.g. `"en"`, `"vi"`) and not
  resolved to a full locale object at registration time.
- `PhoneNumber` is stored as the normalised string provided by the user; no
  carrier or PSTN lookup is performed at registration.
- The `UserInformation` grouping applies to the API request payload structure;
  the persistence layer may store fields flat on the `User` entity.
- The existing `Name` field (currently in `RegisterRequest` but not persisted
  to `User`) is removed; no migration of historical name data is required as
  the field was never stored.
- Mobile support and internationalisation beyond storing the language preference
  are out of scope for this change.
- Password complexity rules remain unchanged from the current implementation.
