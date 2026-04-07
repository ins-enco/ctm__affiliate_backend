# Contract: POST /api/auth/register

**Version**: 2.0.0 (breaking — new required fields, removed `name`, restructured body, removed JWT from response)
**Branch**: `001-update-register-api`

---

## Request

**Method**: `POST`
**Path**: `/api/auth/register`
**Content-Type**: `application/json`

### Body

```json
{
  "userInformation": {
    "firstName": "Nguyen",
    "lastName": "Nam",
    "email": "nam@example.com",
    "phoneNumber": "+84901234567",
    "language": "vi"
  },
  "password": "Secure@123",
  "confirmPassword": "Secure@123"
}
```

> `sessionId` is NOT accepted in the request body. It is read from the `aff_sid` cookie
> (or configured cookie name) by the controller and injected into the service call.

### Field Rules

| Field | Required | Rules |
|---|---|---|
| `userInformation.firstName` | yes | max 50 chars |
| `userInformation.lastName` | yes | max 50 chars |
| `userInformation.email` | yes | valid email format; must be unique |
| `userInformation.phoneNumber` | yes | E.164 format e.g. `+84901234567` |
| `userInformation.language` | yes | BCP 47 code e.g. `en`, `vi`, `en-US` |
| `password` | yes | min 8 chars, uppercase, digit, special char |
| `confirmPassword` | yes | must exactly match `password` |

---

## Responses

### 201 Created — Success

```json
{
  "userId": 42,
  "email": "nam@example.com"
}
```

> No token is returned. The client must call `POST /api/auth/login` to obtain a JWT.
> Registration and authentication are separate concerns.

### 400 Bad Request — Validation failure

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "UserInformation.PhoneNumber": ["PhoneNumber must be a valid international phone number."],
    "ConfirmPassword": ["Passwords do not match."]
  }
}
```

### 409 Conflict — Email already registered

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Conflict",
  "status": 409,
  "detail": "Email already registered."
}
```

---

## Breaking changes vs v1

| v1 field | v2 |
|---|---|
| `name` (root) | Removed. Replaced by `userInformation.firstName` + `userInformation.lastName` |
| `email` (root) | Moved to `userInformation.email` |
| `password` (root) | Unchanged (stays at root) |
| — | `userInformation.phoneNumber` (new, required) |
| — | `userInformation.language` (new, required) |
| — | `confirmPassword` (new, required) |
| Response: `token`, `expiresAt`, `affiliateId` | Removed. Response is now `{ userId, email }` only. Use `POST /api/auth/login` to obtain a JWT. |
