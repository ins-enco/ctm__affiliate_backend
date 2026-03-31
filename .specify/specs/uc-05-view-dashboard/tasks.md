# Tasks: UC-05 — View Affiliate Dashboard

Status: COMPLETED
Origin: milestone-1 (T040–T047)

---

## Phase 1 — Domain

- [x] T05-1 Create `Affiliate` entity (Id, UserId, UniqueCode, Name, extends BaseEntity) in `Affiliate.Domain/Entities/`
- [x] T05-2 Create specifications in `Affiliate.Domain/Specifications/`:
  - `AffiliateByCodeSpecification`
  - `AffiliateByIdSpecification`
  - `AffiliateByUserIdSpecification`

---

## Phase 2 — Infrastructure

- [x] T05-3 Create `AffiliateDbContext` (Affiliates DbSet, UNIQUE on UniqueCode) in `Affiliate.Infrastructure/`
- [x] T05-4 Add EF migration for `affiliates` table

---

## Phase 3 — Application Layer

- [x] T05-5 Implement `AffiliateLookupService` (implements `IAffiliateLookupService`):
  - `CreateAffiliateAsync`: generate unique 8-char alphanumeric code (retry until unique) → save → return `affiliateId` + code
  - `GetAffiliateIdByUserIdAsync`, `FindByCodeAsync`, `FindByIdAsync`
- [x] T05-6 Create `IClickStatsReader` + `ClickStatsReader` in `Tracking.Application/`:
  - `TotalClicks(affiliateId)` — via `ClicksByAffiliateSpecification`
  - `UniqueClicks(affiliateId)` — count of distinct sessions
  - `Last7DayClicks(affiliateId)` — via `RecentClicksSpecification` (cutoff = UtcNow - 7 days)
- [x] T05-7 Create DTOs: `DashboardResult { AffiliateName, UniqueCode, TotalClicks, UniqueClicks, Last7DayClicks, CachedClickCount }` in `Affiliate.Application/DTOs/`
- [x] T05-8 Implement `IAffiliateDashboardService` + `AffiliateDashboardService.GetDashboardAsync()`:
  - Load affiliate via `AffiliateByIdSpecification` → throw `KeyNotFoundException` if missing
  - Read stats via `IClickStatsReader`
  - `cachedClickCount` = `cache.GetOrCreateAsync("affiliate:clickcount:{id}", () => db count, 5-min TTL)`
  - Return `DashboardResult`

---

## Phase 4 — API Layer

- [x] T05-9 Create `AffiliateController` with `GET /api/affiliate/dashboard` (`[Authorize]`):
  - Extract `affiliateId` from JWT claims
  - Call `AffiliateDashboardService.GetDashboardAsync(affiliateId)`
  - Return 200
- [x] T05-10 Create `AffiliateModule.RegisterServices()` — wire AffiliateLookupService, AffiliateDashboardService, DbContext

---

## Phase 5 — Tests

- [x] T05-11 Unit: `GetDashboard_WithValidAffiliate_ReturnsDashboardResult`
- [x] T05-12 Unit: `GetDashboard_AffiliateNotFound_ThrowsKeyNotFoundException`
- [x] T05-13 Unit: `AffiliateLookupService_CreateAffiliateAsync_GeneratesUniqueCode` (7 cases)
- [x] T05-14 Integration: `GET /api/affiliate/dashboard` with valid JWT → 200 + all fields
- [x] T05-15 Integration: `GET /api/affiliate/dashboard` no JWT → 401

---

## Dependencies

- UC-03 tasks must be complete (`IClickStatsReader` reads from `ClickEvents`)
- `IAffiliateLookupService` (Shared) used by UC-01 and UC-03 must match this implementation
