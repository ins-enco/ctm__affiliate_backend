# Data Model: Mock Module — Dashboard API

**Phase 1 — Design**  
**Date**: 2026-04-15  
**Feature**: [spec.md](spec.md)

## Overview

All five data shapes are **read-only DTOs** representing in-memory mock data. There is no persistence layer, no EF context, and no database migrations. All types are C# `record` types with positional parameters.

---

## DTO Definitions

### 1. UserDto

Represents a platform user in the dropdown search list (FR-001).

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | positive, unique | Integer identifier |
| `Name` | `string` | non-empty | Display name |
| `Role` | `string` | one of: `Client`, `Signal Provider`, `Affiliate` | FR-007 |

**C# record**:
```csharp
public record UserDto(int Id, string Name, string Role);
```

**Mock data requirements**: At least 5 records; all three role values must appear at least once (FR-007, SC-003).

---

### 2. CurrentUserDto

Represents the currently logged-in dashboard user shown in the header (FR-002).

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | positive | Integer identifier |
| `Name` | `string` | non-empty | Display name |
| `Abbreviation` | `string` | exactly 2 characters, uppercase | First letter of first + last name; for single-word names: first 2 letters |
| `Role` | `string` | one of: `Client`, `Signal Provider`, `Affiliate` | — |

**C# record**:
```csharp
public record CurrentUserDto(int Id, string Name, string Abbreviation, string Role);
```

**Mock data requirement**: Exactly 1 instance returned.

---

### 3. ClientRequestDto

Represents a client's subscription or strategy request (FR-003).

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Timestamp` | `DateTime` | UTC, ISO 8601 | Date/time of request |
| `Name` | `string` | non-empty | Client display name |
| `Equity` | `decimal` | > 0 | Monetary amount in USD (no currency symbol) |
| `Strategy` | `string` | non-empty | Strategy name |
| `StrategyLicense` | `string` | non-empty, short string | License identifier; format not validated by API |

**C# record**:
```csharp
public record ClientRequestDto(
    DateTime Timestamp,
    string Name,
    decimal Equity,
    string Strategy,
    string StrategyLicense);
```

**Mock data requirement**: Exactly 10 records (FR-003, SC-002).

---

### 4. SignalProviderRequestDto

Represents a signal provider's KYC or onboarding request (FR-004).

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Timestamp` | `DateTime` | UTC, ISO 8601 | Date/time of request |
| `Name` | `string` | non-empty | Signal provider display name |
| `KycStatus` | `string` | one of: `Pending`, `Verified`, `Rejected` | FR-008 |

**C# record**:
```csharp
public record SignalProviderRequestDto(DateTime Timestamp, string Name, string KycStatus);
```

**Mock data requirement**: Exactly 10 records (FR-004, SC-002).

---

### 5. AffiliateRequestDto

Represents an affiliate's KYC or onboarding request (FR-005).

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Timestamp` | `DateTime` | UTC, ISO 8601 | Date/time of request |
| `Name` | `string` | non-empty | Affiliate display name |
| `KycStatus` | `string` | one of: `Pending`, `Verified`, `Rejected` | FR-008 |

**C# record**:
```csharp
public record AffiliateRequestDto(DateTime Timestamp, string Name, string KycStatus);
```

**Mock data requirement**: Exactly 10 records (FR-005, SC-002).

---

## Validation Rules

All validation is enforced by the static mock data itself — no runtime input validation is required (endpoints accept no query parameters). Constraints are verified by unit and integration tests.

| Constraint | Enforced by |
|------------|-------------|
| User list covers all 3 roles | Static data + unit test |
| Abbreviation exactly 2 chars | Static data + unit test |
| Client requests exactly 10 | Static data + unit test |
| Equity > 0 | Static data + unit test |
| Signal provider requests exactly 10 | Static data + unit test |
| kycStatus ∈ {Pending, Verified, Rejected} | Static data + unit test |
| Affiliate requests exactly 10 | Static data + unit test |
| All timestamps UTC ISO 8601 | Static data (`DateTime` with `DateTimeKind.Utc`) |

---

## State Transitions

N/A — all data is read-only static mock data. No state changes occur.

---

## Namespace

```
Mock.Application.DTOs
```
