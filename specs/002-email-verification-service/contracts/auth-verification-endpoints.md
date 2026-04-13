# API Contracts: Email Verification Endpoints

**Module**: Auth | **Branch**: `feature/002-email-verification-service`

---

## POST /api/auth/verify-email

Verifies a user's email address using the token from the verification email.

### Request

```http
POST /api/auth/verify-email
Content-Type: application/json

{
  "token": "string"   // required, the verification token from the email link
}
```

### Responses

**200 OK** — Token valid and email verified

```json
{
  "message": "Email verified successfully."
}
```

**400 Bad Request** — Token missing or malformed

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "Token is required."
}
```

**409 Conflict** — Email already verified

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Conflict",
  "status": 409,
  "detail": "Email address is already verified."
}
```

**410 Gone** — Token expired or already consumed

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Gone",
  "status": 410,
  "detail": "Verification token has expired or has already been used."
}
```

---

## POST /api/auth/resend-verification

Sends a new verification email to the authenticated or identified unverified account.

### Request

```http
POST /api/auth/resend-verification
Content-Type: application/json

{
  "email": "string"   // required, the registered email address
}
```

### Responses

**200 OK** — New verification email dispatched

```json
{
  "message": "Verification email sent."
}
```

**409 Conflict** — Account is already verified

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Conflict",
  "status": 409,
  "detail": "Email address is already verified."
}
```

**429 Too Many Requests** — Rate limit hit (within 2-minute window)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "A verification email was recently sent. Please wait before requesting another."
}
```

**404 Not Found** — Email not registered

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "No account found with that email address."
}
```

---

## Notes

- Both endpoints are **unauthenticated** (no JWT required) — users who haven't verified cannot obtain a JWT.
- Error shape follows RFC 7807 ProblemDetails (existing project standard).
- `POST /api/auth/register` response is unchanged — it already succeeds; verification email is dispatched asynchronously via the event system.
