# Feature: UC-03 — Record Click

## Overview

When a visitor follows a referral link containing an affiliate code, the system records a unique click using SHA-256 session fingerprinting with a monthly attribution window. Duplicate clicks within the same session or same calendar month are suppressed.

---

## User Story

**As a** visitor clicking a referral link
**I want** my click to be recorded once per session per month
**So that** the affiliate is credited with accurate unique click counts

---

## Acceptance Criteria

- [ ] GET /api/tracking/click?affiliateCode=XXX → 200 with `{ isUnique, affiliateCode, sessionId, message }`
- [ ] GET /api/tracking/click with an unknown affiliate code → 404 Not Found (ProblemDetails)
- [ ] First click from a new session → `isUnique = true`, click saved, `aff_sid` cookie set (1-day TTL)
- [ ] Repeat click with `aff_sid` cookie present → `isUnique = false`, no duplicate saved
- [ ] Same IP + UserAgent + affiliateCode within the same calendar month (no cookie) → `isUnique = false` (DB unique index blocks insert)
- [ ] Same IP + UserAgent + affiliateCode in a new calendar month → `isUnique = true` (different SHA-256 hash)
- [ ] Session ID formula: `SHA256(IPAddress + UserAgent + AffiliateCode + "yyyy-MM")`
- [ ] Affiliate lookup is cached for 10 minutes via `ICacheService`
- [ ] Cache key `affiliate:clickcount:{affiliateId}` is invalidated on every unique click

---

## Session Fingerprinting Details

- The monthly bucket (`"yyyy-MM"`) is produced by `protected virtual GetAttributionBucket()` to allow test overrides without mocking system time globally.
- Cookie name: `aff_sid`, lifetime: 1 day (configurable).
- Uniqueness enforced by a DB-level unique index on `(AffiliateId, SessionId)` in `ClickEvent`.

---

## Out of Scope

- Geolocation or device-type enrichment
- Click fraud detection
- Redis-backed caching (MemoryCache only in Milestone 1)

---

## Related Use Cases

| ID | Name | Relation |
|---|---|---|
| UC-04 | Record Conversion | Conversion lookup requires a prior `ClickEvent` by `sessionId` |
| UC-06 | Auto-Record Registration Conversion | `aff_sid` cookie set here is read at registration |
| UC-05 | View Dashboard | `totalClicks`, `uniqueClicks`, `last7DayClicks` are derived from click records |
