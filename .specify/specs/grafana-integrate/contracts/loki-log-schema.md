# Contract: Loki Log Schema

## Overview
Defines the structured log events the CopyTradeMarket API emits to Loki. Any change to property names or values is a breaking change to this contract and requires updating both the emitting code and any dependent LogQL queries.

---

## Loki Push Endpoint
- **Config key**: `Loki:Uri`
- **Source**: Environment variable — never hardcoded
- **Protocol**: HTTP/JSON (handled by `Serilog.Sinks.Grafana.Loki`)

---

## Stream Labels

```json
{
  "app": "copytrade-api",
  "environment": "Development | Production",
  "level": "Information | Warning | Error | Fatal"
}
```

---

## Business Event Schemas

### ClickRecorded
Emitted in `TrackingService.RecordClickAsync` after successful `SaveChangesAsync`.

```json
{
  "Level": "Information",
  "Properties": {
    "EventType": "ClickRecorded",
    "AffiliateCode": "<8-char code>",
    "SessionId": "<SHA256 hex>"
  }
}
```

### ConversionRecorded
Emitted in `TrackingService.RecordConversionAsync` after successful `SaveChangesAsync`.

```json
{
  "Level": "Information",
  "Properties": {
    "EventType": "ConversionRecorded",
    "AffiliateCode": "<8-char code>",
    "SessionId": "<SHA256 hex>",
    "ConversionType": "Registration | Deposit"
  }
}
```

---

## Guaranteed Absent Fields

These properties MUST NOT appear in any log entry at `Information` level or above:

- `IpAddress`
- `UserAgent`
- `Email`
- `Password`

Enforced by unit test `LokiLogSchemaTests` using `Serilog.Sinks.TestCorrelator`.
