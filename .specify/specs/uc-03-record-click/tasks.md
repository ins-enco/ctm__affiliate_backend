# Tasks: UC-03 — Record Click

Status: COMPLETED
Origin: milestone-1 (T026–T032)

---

## Phase 1 — Domain

- [x] T03-1 Create `ClickEvent` entity (BIGINT Id, AffiliateId, SessionId, IPAddress, UserAgent, ClickedAt) in `Tracking.Domain/Entities/`
- [x] T03-2 Create specifications in `Tracking.Domain/Specifications/`:
  - `ClickByAffiliateAndSessionSpecification` — lookup before insert (dedup layer 2)
  - `ClicksByAffiliateSpecification` — all clicks for an affiliate
  - `RecentClicksSpecification` — clicks since a cutoff date

---

## Phase 2 — Infrastructure

- [x] T03-3 Create `TrackingDbContext` with `ClickEvents` DbSet:
  - UNIQUE index on `(AffiliateId, SessionId)`
  - INDEX on `ClickedAt`
- [x] T03-4 Add EF migration for `click_events` table

---

## Phase 3 — Application Layer

- [x] T03-5 Create DTOs: `ClickResult { IsUnique, AffiliateCode, SessionId, Message }` in `Tracking.Application/DTOs/`
- [x] T03-6 Implement `ITrackingService` + `TrackingService.RecordClickAsync()`:
  - Lookup affiliate via `IAffiliateLookupService.FindByCodeAsync()` (cached 10-min TTL, `ICacheService`)
  - Throw `KeyNotFoundException` if code not found
  - Use `sessionId` from cookie if present; else compute `SHA256(IP + UA + code + bucket)`
  - `GetAttributionBucket()` → `protected virtual` → `DateTime.UtcNow.ToString("yyyy-MM")`
  - Insert `ClickEvent`; catch `DbUpdateException` (unique constraint) → `IsUnique = false`
  - On unique insert: invalidate cache key `affiliate:clickcount:{affiliateId}`
  - Return `ClickResult` with `SessionId`

---

## Phase 4 — API Layer

- [x] T03-7 Implement `TrackingController.RecordClick()` (`GET /api/tracking/click?affiliateCode=XXX`):
  - Read `aff_sid` cookie → pass as existing `sessionId`
  - Call `TrackingService.RecordClickAsync()`
  - Set `aff_sid` HttpOnly cookie to `result.SessionId` (1-day lifetime)
  - Return 200

---

## Phase 5 — Tests

- [x] T03-8 Unit: `RecordClick_NewSession_ReturnsIsUniqueTrue`
- [x] T03-9 Unit: `RecordClick_WithExistingCookie_ReturnsIsUniqueFalse`
- [x] T03-10 Unit: `RecordClick_SameSessionSameMonth_DbRejectsInsert_ReturnsIsUniqueFalse`
- [x] T03-11 Unit: `RecordClick_UnknownAffiliateCode_ThrowsKeyNotFoundException`
- [x] T03-12 Unit: `RecordClick_SameSessionNewMonth_ReturnsIsUniqueTrue` (bucket override via subclass)
- [x] T03-13 Integration: `GET /api/tracking/click` → 200, `aff_sid` cookie set
- [x] T03-14 Integration: `GET /api/tracking/click` unknown code → 404
- [x] T03-15 Integration (AttributionWindowTests): same bucket → duplicate; new bucket → unique

---

## Dependencies

- Affiliate module (`AffiliateLookupService.FindByCodeAsync()`) must exist before T03-6
- Shared kernel (ICacheService, HashHelper) must exist
