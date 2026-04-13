# API Contract: Subscription History

**Branch**: `004-subscription-history-list`  
**Module**: SubscriptionHistory  
**Base path**: `/api/subscription-history`

---

## GET /api/subscription-history

Returns a list of subscription history records. Pagination is optional — omit query parameters to receive all records.

### Request

```
GET /api/subscription-history
GET /api/subscription-history?page=1&pageSize=10
```

**Headers**

| Header         | Required | Value              |
|----------------|----------|--------------------|
| `Accept`       | No       | `application/json` |

**Query Parameters**

| Parameter  | Type    | Required | Default | Description                                     |
|------------|---------|----------|---------|-------------------------------------------------|
| `page`     | integer | No       | `1`*    | 1-based page number. Must be ≥ 1 if provided.   |
| `pageSize` | integer | No       | `20`*   | Records per page. Must be ≥ 1 if provided.      |

\* Defaults apply only when the other pagination parameter is present. When **neither** is provided, all records are returned and both default to null.

---

### Responses

#### 200 OK — Full list (no pagination parameters)

All records returned. `page`, `pageSize`, `totalPages` are absent (null in JSON).

```json
{
  "items": [
    {
      "timestamp": "2021-12-22T21:56:34Z",
      "clientName": "Aleš Chromec",
      "accountNumber": "31028",
      "strategyName": "Super duper Pro FX",
      "equityConnect": 1000000.00,
      "equityDisconnect": null,
      "actionType": "Subscribe"
    },
    {
      "timestamp": "2021-12-22T21:56:34Z",
      "clientName": "Aleš Chromec",
      "accountNumber": "31028",
      "strategyName": "Moneymaster 2000",
      "equityConnect": 50000.00,
      "equityDisconnect": 1000000.00,
      "actionType": "Unsubscribe"
    }
  ],
  "totalCount": 20,
  "page": null,
  "pageSize": null,
  "totalPages": null
}
```

#### 200 OK — Paginated result

```http
GET /api/subscription-history?page=1&pageSize=5
```

```json
{
  "items": [
    {
      "timestamp": "2021-12-22T21:56:34Z",
      "clientName": "Aleš Chromec",
      "accountNumber": "31028",
      "strategyName": "Super duper Pro FX",
      "equityConnect": 1000000.00,
      "equityDisconnect": null,
      "actionType": "Subscribe"
    }
  ],
  "totalCount": 20,
  "page": 1,
  "pageSize": 5,
  "totalPages": 4
}
```

#### 200 OK — Page beyond total (empty items)

```http
GET /api/subscription-history?page=99&pageSize=10
```

```json
{
  "items": [],
  "totalCount": 20,
  "page": 99,
  "pageSize": 10,
  "totalPages": 2
}
```

#### 400 Bad Request — Invalid pagination parameter

Returned when `page` or `pageSize` is zero or negative. Response follows RFC 7807 ProblemDetails.

```http
GET /api/subscription-history?page=0&pageSize=10
```

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "Page number must be greater than 0."
}
```

```http
GET /api/subscription-history?page=1&pageSize=-1
```

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "Page size must be greater than 0."
}
```

---

### Response Schema

#### SubscriptionHistoryResponse

| Field        | Type                           | Nullable | Description                                                   |
|--------------|--------------------------------|----------|---------------------------------------------------------------|
| `items`      | `SubscriptionHistoryItem[]`    | No       | List of records for the current page (or all records)         |
| `totalCount` | `integer`                      | No       | Total records in the full dataset (before any paging)         |
| `page`       | `integer`                      | Yes      | Current page number; `null` when pagination was not requested |
| `pageSize`   | `integer`                      | Yes      | Page size applied; `null` when pagination was not requested   |
| `totalPages` | `integer`                      | Yes      | Total page count; `null` when pagination was not requested    |

#### SubscriptionHistoryItem

| Field              | Type      | Nullable | Description                                                      |
|--------------------|-----------|----------|------------------------------------------------------------------|
| `timestamp`        | `string`  | No       | ISO 8601 UTC datetime of the subscription event                  |
| `clientName`       | `string`  | No       | Display name of the client                                       |
| `accountNumber`    | `string`  | No       | Client account identifier                                        |
| `strategyName`     | `string`  | No       | Name of the trading strategy                                     |
| `equityConnect`    | `number`  | No       | Decimal monetary value at time of subscription                   |
| `equityDisconnect` | `number`  | Yes      | Decimal monetary value at unsubscription; `null` for Subscribe   |
| `actionType`       | `string`  | No       | `"Subscribe"` or `"Unsubscribe"`                                 |

---

### Swagger Registration

The endpoint appears in Swagger UI as:

```
SubscriptionHistory
  GET /api/subscription-history   Get subscription history list
```

XML doc comments on the controller action provide the Swagger description and parameter documentation.

---

### Error Matrix

| Scenario                    | Status | `detail`                              |
|-----------------------------|--------|---------------------------------------|
| `page=0`                    | 400    | `Page number must be greater than 0.` |
| `page=-5`                   | 400    | `Page number must be greater than 0.` |
| `pageSize=0`                | 400    | `Page size must be greater than 0.`   |
| `pageSize=-1`               | 400    | `Page size must be greater than 0.`   |
| Valid request (any mode)    | 200    | —                                     |
