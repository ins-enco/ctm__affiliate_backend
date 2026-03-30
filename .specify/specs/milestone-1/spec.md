# Feature: CopyTradeMarket — Milestone 1

## Overview

An affiliate tracking platform that lets affiliates generate unique referral links, record clicks and conversions (Registration / Deposit), and view attribution dashboards. The system uses SHA-256 session fingerprinting with a monthly attribution window to deduplicate clicks and attribute conversions to the correct affiliate.

The backend is a Modular Monolith (Auth, Tracking, Affiliate modules) built on ASP.NET Core 8. A React mock frontend provides a dev UI for manual testing.

---

## User Stories

### US1 — User Registration & Login (P1)

**As a** guest
**I want to** register with name, email, and password
**So that** I get an affiliate account with a unique tracking code and a JWT to access the dashboard

**Acceptance Criteria:**
- [ ] POST /api/auth/register with new email → 201 with JWT token, expiresAt, affiliateId
- [ ] POST /api/auth/register with duplicate email → 409 Conflict
- [ ] Password is BCrypt-hashed; plaintext is never stored
- [ ] A linked Affiliate record with an 8-char alphanumeric UniqueCode is created on registration
- [ ] POST /api/auth/login with correct credentials → 200 with JWT
- [ ] POST /api/auth/login with wrong email → 401 Unauthorized
- [ ] POST /api/auth/login with wrong password → 401 Unauthorized
- [ ] JWT contains `sub` (userId) and `affiliateId` claims

---

### US2 — Click Tracking & Deduplication (P1)

**As a** visitor clicking a referral link
**I want** my click to be recorded once per session per month
**So that** the affiliate is credited with accurate unique click counts

**Acceptance Criteria:**
- [ ] GET /api/tracking/click?affiliateCode=XXX → 200 with isUnique, affiliateCode, sessionId, message
- [ ] GET /api/tracking/click with unknown code → 404 Not Found
- [ ] First click from a new session → isUnique = true, click saved, aff_sid cookie set
- [ ] Repeat click with aff_sid cookie present → isUnique = false, no duplicate saved
- [ ] Same IP+UA+code within the same calendar month (no cookie) → isUnique = false (DB unique index blocks insert)
- [ ] Same IP+UA+code in a new calendar month → isUnique = true (different SHA-256 hash)
- [ ] Session ID = SHA-256(IP + UserAgent + affiliateCode + "yyyy-MM")
- [ ] Affiliate lookup is cached for 10 minutes (ICacheService)
- [ ] Cache key `affiliate:clickcount:{affiliateId}` is invalidated on every unique click

---

### US3 — Conversion Attribution (P1)

**As the** system (called after a user registers or deposits)
**I want to** record a conversion and attribute it to the affiliate whose link was clicked
**So that** affiliates get credit for referring customers

**Acceptance Criteria:**
- [ ] POST /api/tracking/convert with valid sessionId + conversionType → 201 with isAttributed, affiliateCode, conversionType, message
- [ ] conversionType must be "Registration" or "Deposit" → 400 if invalid
- [ ] Same session + same type submitted twice → 409 Conflict
- [ ] If matching click found by sessionId → isAttributed = true with correct affiliateCode
- [ ] If no matching click found → isAttributed = false, affiliateCode = null
- [ ] Auto-record Registration conversion via Observer Pattern when user registers with aff_sid cookie
- [ ] No conversion recorded if no aff_sid cookie was present at registration

---

### US4 — Affiliate Dashboard (P2)

**As a** logged-in affiliate
**I want to** view my click and conversion statistics
**So that** I can monitor my referral performance

**Acceptance Criteria:**
- [ ] GET /api/affiliate/dashboard with valid JWT → 200 with all stats
- [ ] GET /api/affiliate/dashboard with no/invalid JWT → 401 Unauthorized
- [ ] Response includes: affiliateName, uniqueCode, totalClicks, uniqueClicks, last7DayClicks, cachedClickCount
- [ ] cachedClickCount served from in-memory cache (5-minute TTL)
- [ ] Cache populated from fresh DB read when cold; invalidated on each new unique click
- [ ] Affiliate not found → 404 Not Found

---

## Out of Scope (Milestone 1)

- KYC module
- Commission calculation / payout
- Multi-currency support
- Redis cache (MemoryCache only in Milestone 1)
- Email notifications
- Admin panel

---

## Success Metrics

- Duplicate clicks are rejected at the database level (unique index), even under concurrent load
- The same visitor in a new calendar month counts as a fresh unique click
- A visitor who clicks a link and then registers automatically has their Registration conversion attributed to the affiliate — no manual API call needed
- All endpoints return RFC 7807 ProblemDetails on error
- Stress test at c=1,000 concurrent requests sustains > 10,000 RPS on click endpoint

---

## Use Cases

| ID | Name | Actor |
|---|---|---|
| UC-01 | User Registration | Guest |
| UC-02 | User Login | Registered user |
| UC-03 | Record Click | Visitor |
| UC-04 | Record Conversion | System / Backend |
| UC-05 | View Dashboard | Logged-in affiliate |
| UC-06 | Auto-Record Registration Conversion | System (Observer Pattern) |
