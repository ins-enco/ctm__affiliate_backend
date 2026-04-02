# Contract: Loki Log Schema

## Overview
This contract defines the structured log event format that the CopyTradeMarket API emits to Grafana Loki. Dashboard queries and alert rules must conform to this schema. Any change to log property names requires updating both the emitting code and the affected dashboards.

---

## Loki Push API
- **Endpoint**: `http://loki:3100/loki/api/v1/push` (internal Docker network)
- **Protocol**: HTTP/JSON (Serilog Loki sink handles serialization)
- **Auth**: None (internal network only)

---

## Log Stream Labels

```json
{
  "app": "copytrade-api",
  "environment": "Development | Production",
  "level": "Information | Warning | Error | Fatal",
  "module": "Auth | Tracking | Affiliate | Host"
}
```

---

## Log Entry Payload (JSON body)

```json
{
  "Timestamp": "2026-04-02T10:00:00.000Z",
  "Level": "Information",
  "MessageTemplate": "Click recorded for affiliate {AffiliateCode} session {SessionId}",
  "RenderedMessage": "Click recorded for affiliate ABC12345 session a3f9...",
  "Properties": {
    "MachineName": "api-host",
    "RequestId": "0HN2ABC:00000001",
    "RequestPath": "/api/tracking/click",
    "StatusCode": 200,
    "Elapsed": 42.5,
    "SourceContext": "Tracking.Application.TrackingService",
    "AffiliateCode": "ABC12345",
    "SessionId": "a3f9c1d2e5b7...",
    "EventType": "ClickRecorded"
  }
}
```

---

## Business Event Schemas

### ClickRecorded
```json
{
  "EventType": "ClickRecorded",
  "AffiliateCode": "<8-char code>",
  "SessionId": "<SHA256 hex>"
}
```

### ConversionRecorded
```json
{
  "EventType": "ConversionRecorded",
  "AffiliateCode": "<8-char code>",
  "SessionId": "<SHA256 hex>",
  "ConversionType": "Registration | Deposit"
}
```

### UserRegistered
```json
{
  "EventType": "UserRegistered",
  "AffiliateCode": "<8-char code>"
}
```

---

## Alert Thresholds (Grafana Alert Rules)

| Alert | Condition | Window | Severity |
|---|---|---|---|
| High Error Rate | error log count > 10 | 5 min | Critical |
| High Latency | P95 Elapsed > 2000ms | 5 min | Warning |
| Error Rate % | errors / total requests > 5% | 5 min | Critical |
