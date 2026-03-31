# Feature: UC-06 — Auto-Record Registration Conversion

## Overview

When a new user registers while carrying an `aff_sid` cookie (set during a prior referral click), the system automatically attributes a `Registration` conversion to the originating affiliate — without requiring a separate API call. This is implemented via the Observer Pattern using domain events.

---

## User Story

**As the** system
**I want to** automatically record a Registration conversion when a referred user registers
**So that** affiliates are credited for registrations without requiring a manual API call

---

## Acceptance Criteria

- [ ] When a user registers with an active `aff_sid` cookie → a `Registration` conversion is automatically recorded and attributed to the correct affiliate
- [ ] When a user registers without an `aff_sid` cookie → no conversion is recorded
- [ ] The Auth module publishes a `UserRegisteredEvent` containing the `sessionId` from the `aff_sid` cookie (if present)
- [ ] The Tracking module's `UserRegisteredEventHandler` consumes the event and calls the conversion logic (UC-04)
- [ ] The Auth module has no direct dependency on the Tracking module — communication is only through `IEventPublisher` in `Shared`
- [ ] If attribution fails (no matching `ClickEvent`), the registration itself still succeeds; no error is surfaced to the user

---

## Implementation Pattern

- **Event**: `UserRegisteredEvent { UserId, SessionId? }` published by `AuthService` after successful registration
- **Handler**: `UserRegisteredEventHandler` in `Tracking.Application` consumes the event
- **Condition**: Handler only proceeds if `SessionId` is non-null
- **Result**: Delegates to the same conversion logic as UC-04 (POST /api/tracking/convert equivalent)

---

## Out of Scope

- Auto-record Deposit conversions (must be triggered explicitly via UC-04)
- Retry logic if the event handler fails

---

## Related Use Cases

| ID | Name | Relation |
|---|---|---|
| UC-01 | User Registration | Trigger: `UserRegisteredEvent` is published here |
| UC-03 | Record Click | `aff_sid` cookie (set here) is read at registration |
| UC-04 | Record Conversion | Handler reuses this conversion logic internally |
