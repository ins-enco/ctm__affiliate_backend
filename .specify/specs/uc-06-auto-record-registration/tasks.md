# Tasks: UC-06 — Auto-Record Registration Conversion

Status: COMPLETED
Origin: milestone-1 (T037–T038)

---

## Phase 1 — Application Layer

- [x] T06-1 Create `UserRegisteredEventHandler` in `Tracking.Application/`:
  - Implements `IEventHandler<UserRegisteredEvent>`
  - `HandleAsync`: if `event.SessionId` is null → return immediately (no cookie was present)
  - Call `TrackingService.RecordConversionAsync(sessionId, "Registration", userId)`
  - Registration succeeds even if attribution fails (exception swallowed or logged, not re-thrown)

---

## Phase 2 — Wiring

- [x] T06-2 Register `UserRegisteredEventHandler` as `IEventHandler<UserRegisteredEvent>` in `TrackingModule.RegisterServices()`
  — ensures `EventPublisher` resolves it from DI when `UserRegisteredEvent` is published

---

## Phase 3 — Tests

- [x] T06-3 Unit: `HandleAsync_WithSessionId_CallsRecordConversion`
- [x] T06-4 Unit: `HandleAsync_WithNullSessionId_DoesNotCallRecordConversion`
- [x] T06-5 Unit: `HandleAsync_ConversionFails_DoesNotThrow` (registration still succeeds)
- [x] T06-6 Integration (ObserverPatternTests): `POST /api/auth/register` with `aff_sid` cookie → Registration conversion auto-recorded
- [x] T06-7 Integration (ObserverPatternTests): `POST /api/auth/register` without `aff_sid` cookie → no conversion recorded

---

## Dependencies

- UC-01 (`UserRegisteredEvent` published in `AuthService.RegisterAsync()`) must exist
- UC-04 (`TrackingService.RecordConversionAsync()`) must exist
- `IEventPublisher` / `IEventHandler<T>` (Shared) must be registered in DI
