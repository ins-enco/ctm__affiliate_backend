# Feature: UC-05 — View Affiliate Dashboard

## Overview

A logged-in affiliate can view their referral performance statistics including total and unique click counts, a 7-day click trend, and a cached click count — all secured behind JWT authentication.

---

## User Story

**As a** logged-in affiliate
**I want to** view my click and conversion statistics
**So that** I can monitor my referral performance

---

## Acceptance Criteria

- [ ] GET /api/affiliate/dashboard with a valid JWT → 200 with full stats payload
- [ ] GET /api/affiliate/dashboard with no JWT or an invalid JWT → 401 Unauthorized (ProblemDetails)
- [ ] Response payload includes:
  - `affiliateName` — display name of the affiliate
  - `uniqueCode` — the 8-char referral code
  - `totalClicks` — all-time click count
  - `uniqueClicks` — deduplicated click count
  - `last7DayClicks` — clicks in the last 7 calendar days
  - `cachedClickCount` — click count served from in-memory cache (5-minute TTL)
- [ ] `cachedClickCount` is populated from a fresh DB read when the cache is cold
- [ ] `cachedClickCount` cache is invalidated on each new unique click (UC-03)
- [ ] Affiliate record not found for the JWT `affiliateId` claim → 404 Not Found (ProblemDetails)

---

## Caching Details

- Cache key: `affiliate:clickcount:{affiliateId}`
- TTL: 5 minutes (MemoryCache; configurable)
- Invalidated by: UC-03 (unique click recorded)

---

## Out of Scope

- Conversion statistics on the dashboard (Milestone 1 scope)
- Date-range filtering
- CSV / export
- Admin view across all affiliates

---

## Related Use Cases

| ID | Name | Relation |
|---|---|---|
| UC-02 | User Login | JWT required to call this endpoint |
| UC-03 | Record Click | Source of click data displayed here; invalidates cache |
