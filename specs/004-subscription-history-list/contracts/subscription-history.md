# API Contract: Subscription History

**Branch**: `004-subscription-history-list`  
**Module**: SubscriptionHistory  
**Base path**: `/api/subscription-history`

---

## GET /api/subscription-history

Returns a list of subscription history records. Filtering, ordering, and pagination are all optional.

### Request

```
GET /api/subscription-history
GET /api/subscription-history?page=1&pageSize=10
GET /api/subscription-history?query=Alice
GET /api/subscription-history?orderBy=clientName&orderDirection=asc
GET /api/subscription-history?query=Alpha&orderBy=timestamp&orderDirection=desc&page=1&pageSize=10
```

**Headers**

| Header         | Required | Value              |
|----------------|----------|--------------------|
| `Accept`       | No       | `application/json` |

**Query Parameters**

| Parameter        | Type    | Required | Default | Description |
|------------------|---------|----------|---------|-------------|
| `query`          | string  | No       | —       | Case-insensitive partial match across `clientName`, `accountNumber`, and `strategyName`. |
| `orderBy`        | string  | No       | `timestamp` | Sort field. Allowed values: `timestamp`, `clientName`, `accountNumber`, `strategyName`, `equityConnect`. |
| `orderDirection` | string  | No       | `desc`  | Sort direction. Allowed values: `asc`, `desc`. |
| `page`           | integer | No       | `1`*    | 1-based page number. Must be ≥ 1 if provided. |
| `pageSize`       | integer | No       | `20`*   | Records per page. Must be ≥ 1 if provided. |

\* Pagination defaults apply only when the other pagination parameter is present. When neither `page` nor `pageSize` is provided, all matching records are returned and pagination fields are null.

Ordering rules:
- If `orderBy` is omitted, default field is `timestamp`.
- If `orderDirection` is omitted, default direction is `desc`.
- If only `orderDirection` is provided, it applies to default field `timestamp`.

Processing order:
- Filter first (`query`)
- Order second (`orderBy`, `orderDirection`)
- Paginate last (`page`, `pageSize`)

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

#### 200 OK — Filtered and ordered result

```http
GET /api/subscription-history?query=Alice&orderBy=timestamp&orderDirection=asc
```

```json
{
  "items": [
    {
      "timestamp": "2021-12-20T08:00:00Z",
      "clientName": "Alice Tran",
      "accountNumber": "ACC-001",
      "strategyName": "Alpha Growth",
      "equityConnect": 12000.00,
      "equityDisconnect": null,
      "actionType": "Subscribe"
    }
  ],
  "totalCount": 1,
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
| `totalCount` | `integer`                      | No       | Total records after filtering and before pagination             |
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
| `orderBy=unknown`           | 400    | `Invalid orderBy. Allowed values: ...` |
| `orderDirection=up`         | 400    | `Invalid orderDirection. Allowed values: asc, desc.` |
| Valid request (any mode)    | 200    | —                                     |
