# Feature: UC-04 — Record Conversion

## Overview

After a tracked visitor registers or makes a deposit, the system records a conversion event and attributes it to the affiliate whose referral link was originally clicked. Attribution is resolved by matching the `sessionId` to a prior `ClickEvent`.

---

## User Story

**As the** system (called after a user registers or deposits)
**I want to** record a conversion and attribute it to the affiliate whose link was clicked
**So that** affiliates get credit for referring customers

---

## Acceptance Criteria

- [ ] POST /api/tracking/convert with valid `{ sessionId, conversionType }` → 201 with `{ isAttributed, affiliateCode, conversionType, message }`
- [ ] `conversionType` must be `"Registration"` or `"Deposit"` → 400 Bad Request if any other value (ProblemDetails)
- [ ] Same `sessionId` + same `conversionType` submitted twice → 409 Conflict (ProblemDetails)
- [ ] If a `ClickEvent` is found for the given `sessionId` → `isAttributed = true` with correct `affiliateCode`
- [ ] If no matching `ClickEvent` found → `isAttributed = false`, `affiliateCode = null`

---

## Data Rules

- Uniqueness enforced at DB level: unique index on `(AffiliateId, SessionId, ConversionType)` in `ConversionEvent`.
- Both `Registration` and `Deposit` conversions are valid types; no other values accepted.

---

## Out of Scope

- Commission calculation or payout on conversion
- Conversion value / revenue tracking
- Multi-step funnel attribution

---

## Related Use Cases

| ID | Name | Relation |
|---|---|---|
| UC-03 | Record Click | `ClickEvent` must pre-exist for attribution to succeed |
| UC-06 | Auto-Record Registration Conversion | Automated path that calls this logic via Observer Pattern |
