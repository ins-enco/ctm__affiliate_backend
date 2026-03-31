# Feature: UC-01 — User Registration

## Overview

A guest can register with name, email, and password. On success, the system creates a User record, generates a linked Affiliate record with a unique 8-character alphanumeric code, and returns a JWT for immediate API access.

---

## User Story

**As a** guest
**I want to** register with name, email, and password
**So that** I get an affiliate account with a unique tracking code and a JWT to access the dashboard

---

## Acceptance Criteria

- [ ] POST /api/auth/register with a new email → 201 with `{ token, expiresAt, affiliateId }`
- [ ] POST /api/auth/register with a duplicate email → 409 Conflict (ProblemDetails)
- [ ] Password is BCrypt-hashed; plaintext is never stored or logged
- [ ] A linked `Affiliate` record with an 8-char alphanumeric `UniqueCode` is created atomically on registration
- [ ] JWT contains `sub` (userId) and `affiliateId` claims

---

## Out of Scope

- Email verification
- KYC / identity checks
- Admin-created accounts

---

## Related Use Cases

| ID | Name | Relation |
|---|---|---|
| UC-02 | User Login | Same Auth module; shares `User` entity |
| UC-06 | Auto-Record Registration Conversion | Fires `UserRegisteredEvent` consumed by Tracking |
