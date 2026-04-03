# Contract: Validation Error Response

## Overview

When a request body fails attribute validation, the API returns `403 Forbidden` with a `ProblemDetails`-compatible JSON body. This contract applies to every endpoint that receives an annotated DTO.

---

## Response Shape

### Success (no violations)
Request proceeds normally. No change to response shape.

---

### Failure (one or more violations)

**HTTP Status:** `403 Forbidden`  
**Content-Type:** `application/problem+json`

```json
{
  "status": 403,
  "title": "Validation Failed",
  "errors": {
    "<camelCaseFieldName>": [
      "<human-readable rule violation>",
      "<additional violation if multiple rules failed>"
    ]
  }
}
```

**Rules:**
- `errors` contains only fields that have at least one violation
- Each field maps to an array of one or more plain-language error strings
- Field names in `errors` use camelCase (matching JSON serialisation conventions)
- `detail` is omitted (violations are expressed in `errors`, not `detail`)

---

## Examples

### POST /api/auth/register — All fields invalid
**Request:**
```json
{
  "name": "",
  "email": "not-an-email",
  "password": "weak"
}
```

**Response 403:**
```json
{
  "status": 403,
  "title": "Validation Failed",
  "errors": {
    "name": ["Name is required"],
    "email": ["Email must be a valid email address"],
    "password": [
      "Password must be at least 8 characters",
      "Password must contain at least one uppercase letter",
      "Password must contain at least one digit",
      "Password must contain at least one special character"
    ]
  }
}
```

---

### POST /api/auth/login — Email malformed only
**Request:**
```json
{
  "email": "bad-email",
  "password": "ValidPass1!"
}
```

**Response 403:**
```json
{
  "status": 403,
  "title": "Validation Failed",
  "errors": {
    "email": ["Email must be a valid email address"]
  }
}
```

---

## Filter Behaviour

The `ValidationActionFilter` (global `IAsyncActionFilter`) applies this contract:

1. For each action argument that is a non-primitive object, run `DtoValidator.Validate()`
2. If the combined `errors` dictionary is non-empty, write the 403 response and **do not call `next()`**
3. If `errors` is empty, call `next()` — no modification to the pipeline
