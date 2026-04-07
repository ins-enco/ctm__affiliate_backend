# Feature Specification: Email Verification and Mail Service

**Feature Branch**: `feature/002-email-verification-service`
**Created**: 2026-04-07
**Status**: Draft
**Input**: After register done, need to verify email. Create mail service that can use template in many datasources. Use observation/event pattern for email dispatch.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Verify Email After Registration (Priority: P1)

A newly registered user receives a verification email and must confirm their email address to fully activate their account. Until verified, the account exists but access may be restricted.

**Why this priority**: Email verification is the core deliverable. Without it, the entire feature has no value. It is the minimum viable outcome.

**Independent Test**: Register a new account, check inbox for a verification email, click the link, and confirm the account status changes from "unverified" to "verified."

**Acceptance Scenarios**:

1. **Given** a user successfully completes registration, **When** registration is confirmed, **Then** the system dispatches a verification email to the registered address within 60 seconds.
2. **Given** a user receives a verification email, **When** they click the verification link before it expires, **Then** their account status is updated to verified and they are granted full access.
3. **Given** a user clicks a verification link that has already been used, **When** the link is submitted, **Then** the system informs the user the link is already consumed and offers to resend a new one.
4. **Given** a user's verification link has expired (after the configured expiry duration), **When** they click it, **Then** the system informs them it has expired and offers to resend a new one.
5. **Given** a verified user tries to re-verify, **When** attempting verification, **Then** the system informs them their account is already verified.

---

### User Story 2 - Resend Verification Email (Priority: P2)

A user who did not receive or has lost their verification email can request a new one be sent.

**Why this priority**: Users may miss or lose verification emails. Without a resend option, they are permanently locked out, reducing registration completion rates.

**Independent Test**: Trigger a resend request for an unverified account, confirm a new verification email is dispatched, and confirm the old token is invalidated.

**Acceptance Scenarios**:

1. **Given** an unverified user requests a new verification email, **When** the request is submitted, **Then** a new verification email is sent and any previously issued unexpired tokens for that account are invalidated.
2. **Given** an unverified user has requested a resend, **When** fewer than 2 minutes have passed since the last send, **Then** the system declines to send another and informs the user of the wait time (rate limiting).
3. **Given** a verified user requests a resend, **When** the request is submitted, **Then** the system informs them their account is already verified and no email is sent.

---

### User Story 3 - Templated Emails from Multiple Sources (Priority: P3)

Operators can manage email templates from different storage sources (e.g., file system, database). The mail service uses whichever source is configured without requiring code changes.

**Why this priority**: A hardcoded email template is unmaintainable. Template flexibility ensures the service is reusable for future notification types and configurable per deployment environment.

**Independent Test**: Configure two different template sources; confirm the mail service can load and render a template from each, producing the correct subject and body.

**Acceptance Scenarios**:

1. **Given** a template is stored in the configured datasource, **When** the mail service prepares an email, **Then** it loads the correct template and substitutes all placeholders (e.g., recipient name, verification link) with the actual values.
2. **Given** a template datasource is unavailable, **When** the mail service attempts to load a template, **Then** it falls back to the next configured datasource or raises a clear, diagnosable error.
3. **Given** a template with missing placeholders for required values, **When** the mail service attempts to render it, **Then** the system rejects the send and logs the issue rather than sending a broken email.

---

### Edge Cases

- What happens when the registered email address does not exist or bounces? — The system records the delivery failure; the user can request a resend with a corrected address.
- What happens if a user registers the same email twice before verifying? — Subsequent registrations with the same email are blocked (email uniqueness enforced at registration).
- What happens if the mail service is unavailable at time of registration? — The verification dispatch is retried asynchronously; registration itself succeeds so the user is not blocked.
- What happens if no template datasource is configured? — The system fails to start with a clear configuration error, preventing silent failures at runtime.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST send a verification email to the user's registered address immediately after a successful registration.
- **FR-002**: System MUST generate a unique, time-limited verification token per registration, with the expiry duration configurable by operators (default: 24 hours).
- **FR-003**: Users MUST be able to verify their account by following the link in the verification email.
- **FR-004**: System MUST mark the verification token as consumed after a successful verification, preventing reuse.
- **FR-005**: System MUST reject expired or already-consumed verification tokens with a user-legible error.
- **FR-006**: Users MUST be able to request a resend of their verification email at most once every 2 minutes per account.
- **FR-007**: System MUST invalidate all outstanding verification tokens for an account when a resend is triggered.
- **FR-008**: The mail service MUST support loading email templates from multiple configurable datasources (at minimum: file system and database).
- **FR-009**: Email templates MUST support variable substitution for at least: recipient name, verification link, and expiry duration.
- **FR-010**: Email dispatch MUST be triggered via the existing domain event system (observer/publish-subscribe pattern), decoupling it from the registration flow.
- **FR-011**: System MUST NOT block the registration response while the verification email is being sent.
- **FR-012**: System MUST log all email dispatch attempts, outcomes (sent, failed, retried), and template loading events for observability.

### Key Entities

- **EmailVerificationToken**: Represents a single-use time-limited token tied to a user and email address. Key attributes: token value (unique), associated user, target email address, expiry time, consumed flag, creation timestamp.
- **EmailTemplate**: A named template with subject and body containing placeholder markers. Key attributes: template name (unique identifier), subject pattern, body pattern, datasource origin.
- **MailMessage**: A prepared outbound message ready for dispatch. Derived from a template with all placeholders resolved. Key attributes: recipient address, resolved subject, resolved body.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of successful registrations result in a verification email being dispatched within 60 seconds.
- **SC-002**: Verified users can complete the full verification flow (receive email → click link → confirmed) in under 2 minutes under normal conditions.
- **SC-003**: Expired or reused tokens are rejected in 100% of cases with no false positives on valid tokens.
- **SC-004**: The mail service can render templates from at least 2 distinct datasource types with zero code changes between them.
- **SC-005**: Resend requests exceeding the rate limit (more than 1 per 2 minutes per account) are declined in 100% of cases.
- **SC-006**: Email dispatch failure does not degrade registration success rate — registration completion remains unaffected by mail service availability.
- **SC-007**: Changing the verification token expiry duration requires no code deployment — a configuration change alone is sufficient.

---

## Assumptions

- The registration flow already exists and produces a successful outcome (user record created) that can publish a domain event — this feature adds email dispatch as a side effect of that event.
- The existing domain event infrastructure (`IEventPublisher` / `IEventHandler<T>`) will be reused to decouple email dispatch from the Auth module.
- Unverified accounts are created but may have restricted access — the exact access restrictions are defined by the existing Auth module policy and are out of scope for this spec.
- Email delivery infrastructure (SMTP or equivalent) is available in the deployment environment; this spec covers the application-side dispatch only, not delivery configuration.
- Templates are managed by operators outside of the application UI (e.g., via file deployment or direct database entry); a template management UI is out of scope for this feature.
- Rate limiting for resend requests (2-minute window) applies per user account, not per IP address.
- A single verification email format is sufficient for v1; multi-language template support is out of scope.
