# Feature: UC-02 — User Login

## Overview

A registered user authenticates with email and password and receives a JWT to access protected endpoints.

---

## User Story

**As a** registered user
**I want to** log in with my email and password
**So that** I receive a JWT to access my affiliate dashboard

---

## Acceptance Criteria

- [ ] POST /api/auth/login with correct credentials → 200 with `{ token, expiresAt, affiliateId }`
- [ ] POST /api/auth/login with an unregistered email → 401 Unauthorized (ProblemDetails)
- [ ] POST /api/auth/login with a wrong password → 401 Unauthorized (ProblemDetails)
- [ ] JWT contains `sub` (userId) and `affiliateId` claims

---

## Out of Scope

- OAuth / social login
- Multi-factor authentication
- Refresh tokens (Milestone 1)
- Account lockout after failed attempts

---

## Related Use Cases

| ID | Name | Relation |
|---|---|---|
| UC-01 | User Registration | Same Auth module; account must exist first |
| UC-05 | View Dashboard | Dashboard requires valid JWT from this flow |
