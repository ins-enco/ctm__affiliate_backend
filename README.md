# CopyTrade Market

## Overview

CopyTrade Market is an ASP.NET Core 8 backend API for a copy-trading platform. It follows a **modular monolith** architecture — each business domain lives in its own self-contained module but runs in a single deployable host.

### Modules

| Module | Responsibility |
|---|---|
| **Auth** | User registration, login, JWT token issuance |
| **Tracking** | Trade tracking and session management |
| **Affiliate** | Affiliate dashboard and referral management |

### Tech Stack

- **Runtime:** .NET 8 / ASP.NET Core
- **Database:** MySQL 8 (one DbContext per module, auto-migrated on startup)
- **Auth:** JWT Bearer
- **Logging:** Serilog (structured, enriched with machine name)
- **Docs:** Swagger / OpenAPI (JWT-aware UI)
- **Cache:** In-memory cache via `ICacheService` abstraction
- **Frontend:** React (served separately, proxied via Nginx in Docker)

### Project Structure

```
CopyTradeMarket/
├── Backend/
│   ├── src/
│   │   ├── Host/                        # Entry point (Program.cs, Dockerfile)
│   │   ├── Modules/
│   │   │   ├── Auth/                    # Auth.API / Application / Domain / Infrastructure
│   │   │   ├── Tracking/
│   │   │   └── Affiliate/
│   │   └── Shared/                      # Cross-module contracts & utilities
│   ├── docker-compose.yml
│   └── Dockerfile
└── Mock FrontEnd/                       # React frontend (dev/mock UI)
```

---

## Docker

### Services

| Service | Image | Port |
|---|---|---|
| `db` | `mysql:8.0` | `3306` |
| `api` | built from `./Backend` | `5115 → 8080` |
| `frontend` | built from `./Mock FrontEnd` | `3000 → 80` |

The `api` waits for `db` to be healthy before starting. The database is migrated automatically on first boot — no manual migration step needed.

The `frontend` service is gated behind the `frontend` profile and is not started by default.

### Run (API + Database only)

```bash
cd Backend
docker compose up --build
```

API will be available at: `http://localhost:5115/swagger`

### Run (with Frontend)

```bash
cd Backend
docker compose --profile frontend up --build
```

Frontend will be available at: `http://localhost:3000`

### Stop & clean up

```bash
docker compose down          # stop containers
docker compose down -v       # stop + remove volumes (resets database)
```

### Environment Variables (api service)

| Variable | Description |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` enables Swagger and dev seeding |
| `ConnectionStrings__DefaultConnection` | MySQL connection string |
| `JwtSettings__SecretKey` | Secret key for signing JWT tokens |

> For production, override these via a `.env` file or your container orchestration secrets — do not commit real credentials.

---

## Local Development (without Docker)

1. Start a MySQL instance on port `3306` with database `copytrade_db`
2. Update `appsettings.json` with your connection string
3. Run the API:

```bash
cd Backend
dotnet run --project src/Host/CopyTradeMarketApi.Host
```

Browse to `http://localhost:5115/swagger`
