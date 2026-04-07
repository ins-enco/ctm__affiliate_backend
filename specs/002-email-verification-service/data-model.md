# Data Model: Email Verification and Mail Service

**Branch**: `feature/002-email-verification-service` | **Date**: 2026-04-07

---

## New Entities

### EmailVerificationToken

**Table**: `EmailVerificationTokens` (owned by `AuthDbContext`)

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `int` | PK, auto-increment | — |
| `UserId` | `int` | FK → `Users.Id`, not null | Cascade delete |
| `Email` | `string` | not null, max 256 | Snapshot of email at time of issue |
| `Token` | `string` | not null, unique, max 128 | Cryptographically random, URL-safe |
| `ExpiresAt` | `DateTime` | not null | UTC; set to `Now + configured expiry` |
| `ConsumedAt` | `DateTime?` | nullable | Set when token is successfully used |
| `CreatedAt` | `DateTime` | not null | UTC (from `BaseEntity`) |
| `UpdatedAt` | `DateTime` | not null | UTC (from `BaseEntity`) |

**Unique index**: `Token` (enforced at DB level)
**Index**: `(UserId, ConsumedAt)` — for fast lookup of active tokens per user
**Relationships**: Many-to-one with `User`

**State machine**:
```
Issued → [used before expiry] → Consumed
Issued → [expiry reached]     → Expired (derived from ExpiresAt, no stored state)
Issued → [resend triggered]   → Invalidated (ConsumedAt set proactively)
```

---

### EmailTemplate (optional — only if DatabaseTemplateProvider is enabled)

**Table**: `EmailTemplates` (owned by `AuthDbContext`)

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `int` | PK, auto-increment | — |
| `Name` | `string` | not null, unique, max 128 | Lookup key (e.g., `email-verification`) |
| `SubjectPattern` | `string` | not null, max 512 | May contain `{{placeholders}}` |
| `BodyPattern` | `string` | not null | HTML or plain text with `{{placeholders}}` |
| `CreatedAt` | `DateTime` | not null | UTC |
| `UpdatedAt` | `DateTime` | not null | UTC |

**Unique index**: `Name`

---

## Modified Entities

### User (existing)

Add one field:

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `IsEmailVerified` | `bool` | not null, default `false` | Flipped to `true` on successful token use |

**Migration note**: Existing users get `IsEmailVerified = false`. No data loss.

---

## Value Objects / DTOs (not persisted)

### MailMessage (in `CopyTradeMarketApi.Shared`)

```
record MailMessage(
    string To,
    string Subject,
    string Body
)
```

### VerificationEmailContext

Used to render the verification template:

```
record VerificationEmailContext(
    string RecipientName,
    string VerificationLink,
    string ExpiryDescription    // e.g., "24 hours"
)
```

---

## Configuration Additions

### appsettings.json

```json
{
  "EmailVerification": {
    "TokenExpiryHours": 24
  },
  "MailSettings": {
    "FromAddress": "SET_VIA_USER_SECRETS_OR_ENV",
    "FromName": "CopyTradeMarket",
    "SmtpHost": "SET_VIA_USER_SECRETS_OR_ENV",
    "SmtpPort": 587,
    "SmtpUsername": "SET_VIA_USER_SECRETS_OR_ENV",
    "SmtpPassword": "SET_VIA_USER_SECRETS_OR_ENV",
    "UseSsl": true
  },
  "EmailTemplates": {
    "FileSystemPath": "templates/email"
  }
}
```

---

## EF Migrations Required

| Migration name | Change |
|---|---|
| `AddEmailVerificationToken` | Creates `EmailVerificationTokens` table |
| `AddEmailTemplates` | Creates `EmailTemplates` table (only if DB provider enabled) |
| `AddIsEmailVerifiedToUser` | Adds `IsEmailVerified` column to `Users` with default `false` |
