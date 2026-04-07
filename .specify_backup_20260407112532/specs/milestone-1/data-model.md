# Data Model: CopyTradeMarket — Milestone 1

## Overview

Each module owns its own DbContext and tables. All three contexts point to the same MySQL server but never share tables or define cross-module foreign keys. Referential integrity across module boundaries is enforced in application code, not at the database level.

---

## Entity: User (Auth module — `AuthDbContext`)

```csharp
public class User : BaseEntity
{
    public int Id { get; set; }                        // INT PK AUTO_INCREMENT
    public string Email { get; set; }                  // VARCHAR(255) UNIQUE NOT NULL
    public string PasswordHash { get; set; }           // VARCHAR(255) NOT NULL — BCrypt hash
}
```

**Table:** `users`

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, AUTO_INCREMENT |
| Email | VARCHAR(255) | UNIQUE, NOT NULL |
| PasswordHash | VARCHAR(255) | NOT NULL |
| CreatedAt | DATETIME | NOT NULL, UTC |
| UpdatedAt | DATETIME | NOT NULL, UTC |

**Business rules:**
- Email must be globally unique (enforced by DB unique index)
- Password is always BCrypt-hashed before storage (cost factor 12)
- Plaintext password never persisted or logged

---

## Entity: Affiliate (Affiliate module — `AffiliateDbContext`)

```csharp
public class Affiliate : BaseEntity
{
    public int Id { get; set; }                        // INT PK AUTO_INCREMENT
    public int UserId { get; set; }                    // INT NOT NULL — no DB FK, resolved via IAffiliateLookupService
    public string Name { get; set; }                   // VARCHAR(100) NOT NULL
    public string UniqueCode { get; set; }             // VARCHAR(10) UNIQUE NOT NULL — 8-char alphanumeric
    public int ClickCount { get; set; }                // INT DEFAULT 0 — denormalized counter
}
```

**Table:** `affiliates`

| Column | Type | Constraints |
|---|---|---|
| Id | INT | PK, AUTO_INCREMENT |
| UserId | INT | NOT NULL (no DB FK) |
| Name | VARCHAR(100) | NOT NULL |
| UniqueCode | VARCHAR(10) | UNIQUE, NOT NULL |
| ClickCount | INT | DEFAULT 0 |
| CreatedAt | DATETIME | NOT NULL, UTC |
| UpdatedAt | DATETIME | NOT NULL, UTC |

**Business rules:**
- `UniqueCode` is 8-char alphanumeric, auto-generated on registration with retry until unique
- `ClickCount` is a denormalized cache — not the source of truth for dashboard (use `ClicksByAffiliateSpecification` for accurate count)
- One User → One Affiliate (1:1 relationship enforced in application code)

---

## Entity: ClickEvent (Tracking module — `TrackingDbContext`)

```csharp
public class ClickEvent : BaseEntity
{
    public long Id { get; set; }                       // BIGINT PK AUTO_INCREMENT
    public int AffiliateId { get; set; }               // INT NOT NULL — no DB FK
    public string SessionId { get; set; }              // VARCHAR(64) NOT NULL — SHA-256 hex hash
    public string? IPAddress { get; set; }             // VARCHAR(45) — supports IPv4 + IPv6
    public string? UserAgent { get; set; }             // VARCHAR(512)
    public DateTime ClickedAt { get; set; }            // DATETIME NOT NULL, UTC
}
```

**Table:** `click_events`

| Column | Type | Constraints |
|---|---|---|
| Id | BIGINT | PK, AUTO_INCREMENT |
| AffiliateId | INT | NOT NULL (no DB FK) |
| SessionId | VARCHAR(64) | NOT NULL |
| IPAddress | VARCHAR(45) | nullable |
| UserAgent | VARCHAR(512) | nullable |
| ClickedAt | DATETIME | NOT NULL, UTC |
| CreatedAt | DATETIME | NOT NULL, UTC |
| UpdatedAt | DATETIME | NOT NULL, UTC |

**Indexes:**
- `UNIQUE (AffiliateId, SessionId)` — deduplication at DB level, handles race conditions
- `INDEX (ClickedAt)` — speeds up `RecentClicksSpecification` (last 7 days)

**Business rules:**
- `SessionId` = `SHA-256(IPAddress + UserAgent + AffiliateCode + "yyyy-MM")`
- BIGINT used for Id because click volume can overflow INT at scale
- Duplicate insert (same AffiliateId + SessionId) raises `DbUpdateException`, caught and returned as `IsUnique = false`
- `aff_sid` cookie value = exact `SessionId` stored in DB

---

## Entity: ConversionEvent (Tracking module — `TrackingDbContext`)

```csharp
public class ConversionEvent : BaseEntity
{
    public long Id { get; set; }                       // BIGINT PK AUTO_INCREMENT
    public int AffiliateId { get; set; }               // INT NOT NULL — 0 = unattributed
    public string SessionId { get; set; }              // VARCHAR(64) NOT NULL — matches ClickEvent.SessionId
    public int? UserId { get; set; }                   // INT nullable — set for Registration type
    public string ConversionType { get; set; }         // VARCHAR(50) NOT NULL — "Registration" | "Deposit"
    public DateTime ConvertedAt { get; set; }          // DATETIME NOT NULL, UTC
}
```

**Table:** `conversion_events`

| Column | Type | Constraints |
|---|---|---|
| Id | BIGINT | PK, AUTO_INCREMENT |
| AffiliateId | INT | NOT NULL (0 = unattributed) |
| SessionId | VARCHAR(64) | NOT NULL |
| UserId | INT | nullable |
| ConversionType | VARCHAR(50) | NOT NULL |
| ConvertedAt | DATETIME | NOT NULL, UTC |
| CreatedAt | DATETIME | NOT NULL, UTC |
| UpdatedAt | DATETIME | NOT NULL, UTC |

**Indexes:**
- `INDEX (AffiliateId, ConversionType)` — commission aggregation queries
- `INDEX (ConvertedAt)` — time-range reporting
- `INDEX (SessionId)` — fast attribution lookup

**Business rules:**
- `ConversionType` must be exactly `"Registration"` or `"Deposit"` (validated in service layer)
- `AffiliateId = 0` signals unattributed conversion (no matching click found for session)
- Attribution: find latest `ClickEvent` matching `SessionId` → use its `AffiliateId`
- Duplicate prevention: `ConversionBySessionAndTypeSpecification` checked before insert

---

## Base Entity (Shared)

```csharp
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

All entities inherit `CreatedAt` and `UpdatedAt` (UTC, auto-set).

---

## Relationships

```
User (Auth DB)
  └─── 1:1 ──→ Affiliate (Affiliate DB)
                   └─── 1:N ──→ ClickEvent (Tracking DB)
                   └─── 1:N ──→ ConversionEvent (Tracking DB)

ClickEvent.SessionId ──→ ConversionEvent.SessionId  (application-level link)
```

No database-level foreign keys cross module boundaries.

---

## EF Migration Order

1. Auth module → creates `users` table
2. Affiliate module → creates `affiliates` table
3. Tracking module (initial) → creates `click_events` table
4. Tracking module (`AddConversionEvents`) → creates `conversion_events` table

All migrations are idempotent. Auto-applied at startup via `MigrateAsync()` (MySQL provider only — skipped for SQLite in tests).
