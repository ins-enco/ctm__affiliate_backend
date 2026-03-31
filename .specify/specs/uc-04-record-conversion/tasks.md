# Tasks: UC-04 — Record Conversion

Status: COMPLETED
Origin: milestone-1 (T033–T036, T039)

---

## Phase 1 — Domain

- [x] T04-1 Create `ConversionEvent` entity (BIGINT Id, AffiliateId, SessionId, ConversionType, UserId?, ConvertedAt) in `Tracking.Domain/Entities/`
- [x] T04-2 Create specifications in `Tracking.Domain/Specifications/`:
  - `ConversionBySessionAndTypeSpecification` — duplicate check before insert
  - `LatestClickBySessionSpecification` — find click for attribution (ordered desc by ClickedAt)

---

## Phase 2 — Infrastructure

- [x] T04-3 Update `TrackingDbContext` with `ConversionEvents` DbSet:
  - UNIQUE index on `(AffiliateId, SessionId, ConversionType)`
- [x] T04-4 Add EF migration for `conversion_events` table (`AddConversionEvents`)

---

## Phase 3 — Application Layer

- [x] T04-5 Create DTOs: `ConversionRequest { SessionId, ConversionType, UserId? }`, `ConversionResult { IsAttributed, AffiliateCode, ConversionType, Message }` in `Tracking.Application/DTOs/`
- [x] T04-6 Implement `TrackingService.RecordConversionAsync()`:
  - Validate `ConversionType` ∈ `{ "Registration", "Deposit" }` → throw `InvalidOperationException` otherwise
  - Check duplicate via `ConversionBySessionAndTypeSpecification` → throw `ConflictException`
  - Find latest `ClickEvent` via `LatestClickBySessionSpecification`
  - If found → `IsAttributed = true`, save `ConversionEvent` with `AffiliateId` from click
  - If not found → `IsAttributed = false`, `AffiliateCode = null`
  - Return `ConversionResult`

---

## Phase 4 — API Layer

- [x] T04-7 Add `POST /api/tracking/convert` to `TrackingController`:
  - Accept `ConversionRequest` body
  - Call `TrackingService.RecordConversionAsync()`
  - Return 201 on success

---

## Phase 5 — Tests

- [x] T04-8 Unit: `RecordConversion_WithMatchingClick_ReturnsIsAttributedTrue`
- [x] T04-9 Unit: `RecordConversion_NoMatchingClick_ReturnsIsAttributedFalse`
- [x] T04-10 Unit: `RecordConversion_InvalidType_ThrowsInvalidOperationException`
- [x] T04-11 Unit: `RecordConversion_Duplicate_ThrowsConflictException`
- [x] T04-12 Integration: `POST /api/tracking/convert` → 201 with correct attribution
- [x] T04-13 Integration: `POST /api/tracking/convert` invalid type → 400
- [x] T04-14 Integration: `POST /api/tracking/convert` duplicate → 409

---

## Dependencies

- UC-03 tasks must be complete (ClickEvent entity, TrackingDbContext, LatestClickBySessionSpecification)
