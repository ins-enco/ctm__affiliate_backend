# Quickstart: Mock Module — Dashboard API

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)  
**Date**: 2026-04-15

## Overview

Five unauthenticated GET endpoints returning static in-memory mock data for the Dashboard screen. No database, no migrations, no auth setup required.

## Prerequisites

- .NET 8 SDK installed
- Repository cloned and on branch `feature/005-mock-dashboard-api`
- All existing tests passing (`dotnet test` from `Backend/`)

## Module Location

```
Backend/src/Modules/Mock/
├── Mock.API/          ← controller + module registration
└── Mock.Application/  ← DTOs + service interface + static mock data
```

## How to Run

```bash
# From repo root
cd Backend
dotnet run --project src/Host/CopyTradeMarketApi.Host
```

Then hit any of the 5 endpoints:

```bash
curl http://localhost:5000/api/mock/users
curl http://localhost:5000/api/mock/current-user
curl http://localhost:5000/api/mock/client-requests
curl http://localhost:5000/api/mock/signal-provider-requests
curl http://localhost:5000/api/mock/affiliate-requests
```

Or open Swagger UI at `http://localhost:5000/swagger` — all 5 endpoints appear under the `Mock` group.

## How to Test

```bash
# Unit tests (Mock.Application.Tests)
cd Backend
dotnet test tests/Mock.Application.Tests

# Integration tests (Mock endpoints in Integration.Tests)
dotnet test tests/Integration.Tests --filter "Mock"

# Full suite
dotnet test
```

## Key Files

| File | Purpose |
|------|---------|
| [Mock.Application.csproj](../../Backend/src/Modules/Mock/Mock.Application/Mock.Application.csproj) | Application layer project |
| [Mock.API.csproj](../../Backend/src/Modules/Mock/Mock.API/Mock.API.csproj) | API layer project |
| [MockService.cs](../../Backend/src/Modules/Mock/Mock.Application/Services/MockService.cs) | Static mock data + service implementation |
| [MockController.cs](../../Backend/src/Modules/Mock/Mock.API/Controllers/MockController.cs) | 5 GET action methods |
| [MockModule.cs](../../Backend/src/Modules/Mock/Mock.API/MockModule.cs) | Module registration (singleton DI) |

## Endpoint Summary

| Endpoint | Returns | Count |
|----------|---------|-------|
| `GET /api/mock/users` | `UserDto[]` | ≥5 (covers all 3 roles) |
| `GET /api/mock/current-user` | `CurrentUserDto` | 1 object |
| `GET /api/mock/client-requests` | `ClientRequestDto[]` | exactly 10 |
| `GET /api/mock/signal-provider-requests` | `SignalProviderRequestDto[]` | exactly 10 |
| `GET /api/mock/affiliate-requests` | `AffiliateRequestDto[]` | exactly 10 |

## Adding Mock Data

All mock data lives in `MockService.cs` as private static readonly lists. To update the data:

1. Open `MockService.cs`
2. Edit the relevant static list
3. Ensure counts and field constraints remain valid (tests will catch violations)

## Notes

- **DEV-only**: endpoints are only registered when `ASPNETCORE_ENVIRONMENT=Development`. In any other environment all `/api/mock/*` routes return HTTP 404.
- No authentication required — endpoints are open by design (mock layer only)
- Query parameters are silently ignored
- All timestamps are UTC in ISO 8601 format (`DateTime` with `DateTimeKind.Utc`)
- This module will be superseded by real data-backed endpoints in a future iteration
