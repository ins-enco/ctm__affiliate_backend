# Backend Setup Log
Date: 2026-03-20

## Commands Run

### Step 1 — Create Solution
```bash
cd /c/Users/nam.nguyen/Documents/Backend
dotnet new sln -n CopyTradeMarketApi
```

### Step 2 — Create Host project
```bash
dotnet new webapi -n CopyTradeMarketApi.Host --framework net8.0 -o src/Host/CopyTradeMarketApi.Host
dotnet sln add src/Host/CopyTradeMarketApi.Host/CopyTradeMarketApi.Host.csproj
```

### Step 3 — Create Shared Kernel
```bash
dotnet new classlib -n CopyTradeMarketApi.Shared --framework net8.0 -o src/Shared/CopyTradeMarketApi.Shared
dotnet sln add src/Shared/CopyTradeMarketApi.Shared/CopyTradeMarketApi.Shared.csproj
```

### Step 4 — Create Auth module (4 projects)
```bash
dotnet new classlib -n Auth.Domain --framework net8.0 -o src/Modules/Auth/Auth.Domain
dotnet new classlib -n Auth.Application --framework net8.0 -o src/Modules/Auth/Auth.Application
dotnet new classlib -n Auth.Infrastructure --framework net8.0 -o src/Modules/Auth/Auth.Infrastructure
dotnet new classlib -n Auth.API --framework net8.0 -o src/Modules/Auth/Auth.API
dotnet sln add src/Modules/Auth/Auth.Domain/Auth.Domain.csproj
dotnet sln add src/Modules/Auth/Auth.Application/Auth.Application.csproj
dotnet sln add src/Modules/Auth/Auth.Infrastructure/Auth.Infrastructure.csproj
dotnet sln add src/Modules/Auth/Auth.API/Auth.API.csproj
```

### Step 5 — Create Tracking module (4 projects)
```bash
dotnet new classlib -n Tracking.Domain --framework net8.0 -o src/Modules/Tracking/Tracking.Domain
dotnet new classlib -n Tracking.Application --framework net8.0 -o src/Modules/Tracking/Tracking.Application
dotnet new classlib -n Tracking.Infrastructure --framework net8.0 -o src/Modules/Tracking/Tracking.Infrastructure
dotnet new classlib -n Tracking.API --framework net8.0 -o src/Modules/Tracking/Tracking.API
dotnet sln add src/Modules/Tracking/Tracking.Domain/Tracking.Domain.csproj
dotnet sln add src/Modules/Tracking/Tracking.Application/Tracking.Application.csproj
dotnet sln add src/Modules/Tracking/Tracking.Infrastructure/Tracking.Infrastructure.csproj
dotnet sln add src/Modules/Tracking/Tracking.API/Tracking.API.csproj
```

### Step 6 — Create Affiliate module (4 projects)
```bash
dotnet new classlib -n Affiliate.Domain --framework net8.0 -o src/Modules/Affiliate/Affiliate.Domain
dotnet new classlib -n Affiliate.Application --framework net8.0 -o src/Modules/Affiliate/Affiliate.Application
dotnet new classlib -n Affiliate.Infrastructure --framework net8.0 -o src/Modules/Affiliate/Affiliate.Infrastructure
dotnet new classlib -n Affiliate.API --framework net8.0 -o src/Modules/Affiliate/Affiliate.API
dotnet sln add src/Modules/Affiliate/Affiliate.Domain/Affiliate.Domain.csproj
dotnet sln add src/Modules/Affiliate/Affiliate.Application/Affiliate.Application.csproj
dotnet sln add src/Modules/Affiliate/Affiliate.Infrastructure/Affiliate.Infrastructure.csproj
dotnet sln add src/Modules/Affiliate/Affiliate.API/Affiliate.API.csproj
```

### Step 7 — Add project references (Clean Architecture layer rules)
```bash
# Shared has no dependencies

# Auth
dotnet add src/Modules/Auth/Auth.Application/Auth.Application.csproj reference src/Modules/Auth/Auth.Domain/Auth.Domain.csproj src/Shared/CopyTradeMarketApi.Shared/CopyTradeMarketApi.Shared.csproj
dotnet add src/Modules/Auth/Auth.Infrastructure/Auth.Infrastructure.csproj reference src/Modules/Auth/Auth.Application/Auth.Application.csproj
dotnet add src/Modules/Auth/Auth.API/Auth.API.csproj reference src/Modules/Auth/Auth.Application/Auth.Application.csproj src/Modules/Auth/Auth.Infrastructure/Auth.Infrastructure.csproj

# Tracking
dotnet add src/Modules/Tracking/Tracking.Application/Tracking.Application.csproj reference src/Modules/Tracking/Tracking.Domain/Tracking.Domain.csproj src/Shared/CopyTradeMarketApi.Shared/CopyTradeMarketApi.Shared.csproj
dotnet add src/Modules/Tracking/Tracking.Infrastructure/Tracking.Infrastructure.csproj reference src/Modules/Tracking/Tracking.Application/Tracking.Application.csproj
dotnet add src/Modules/Tracking/Tracking.API/Tracking.API.csproj reference src/Modules/Tracking/Tracking.Application/Tracking.Application.csproj src/Modules/Tracking/Tracking.Infrastructure/Tracking.Infrastructure.csproj

# Affiliate
dotnet add src/Modules/Affiliate/Affiliate.Application/Affiliate.Application.csproj reference src/Modules/Affiliate/Affiliate.Domain/Affiliate.Domain.csproj src/Shared/CopyTradeMarketApi.Shared/CopyTradeMarketApi.Shared.csproj
dotnet add src/Modules/Affiliate/Affiliate.Infrastructure/Affiliate.Infrastructure.csproj reference src/Modules/Affiliate/Affiliate.Application/Affiliate.Application.csproj
dotnet add src/Modules/Affiliate/Affiliate.API/Affiliate.API.csproj reference src/Modules/Affiliate/Affiliate.Application/Affiliate.Application.csproj src/Modules/Affiliate/Affiliate.Infrastructure/Affiliate.Infrastructure.csproj

# Host
dotnet add src/Host/CopyTradeMarketApi.Host/CopyTradeMarketApi.Host.csproj reference src/Shared/CopyTradeMarketApi.Shared/CopyTradeMarketApi.Shared.csproj src/Modules/Auth/Auth.API/Auth.API.csproj src/Modules/Tracking/Tracking.API/Tracking.API.csproj src/Modules/Affiliate/Affiliate.API/Affiliate.API.csproj
```

### Step 8 — Install NuGet packages
```bash
# Host
dotnet add src/Host/CopyTradeMarketApi.Host/CopyTradeMarketApi.Host.csproj package Serilog.AspNetCore
dotnet add src/Host/CopyTradeMarketApi.Host/CopyTradeMarketApi.Host.csproj package Serilog.Sinks.Console
dotnet add src/Host/CopyTradeMarketApi.Host/CopyTradeMarketApi.Host.csproj package Serilog.Sinks.File
dotnet add src/Host/CopyTradeMarketApi.Host/CopyTradeMarketApi.Host.csproj package Serilog.Enrichers.Environment
dotnet add src/Host/CopyTradeMarketApi.Host/CopyTradeMarketApi.Host.csproj package Microsoft.AspNetCore.Authentication.JwtBearer

# Auth.Infrastructure
dotnet add src/Modules/Auth/Auth.Infrastructure/Auth.Infrastructure.csproj package Pomelo.EntityFrameworkCore.MySql
dotnet add src/Modules/Auth/Auth.Infrastructure/Auth.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add src/Modules/Auth/Auth.Infrastructure/Auth.Infrastructure.csproj package BCrypt.Net-Next

# Tracking.Infrastructure
dotnet add src/Modules/Tracking/Tracking.Infrastructure/Tracking.Infrastructure.csproj package Pomelo.EntityFrameworkCore.MySql
dotnet add src/Modules/Tracking/Tracking.Infrastructure/Tracking.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design

# Affiliate.Infrastructure
dotnet add src/Modules/Affiliate/Affiliate.Infrastructure/Affiliate.Infrastructure.csproj package Pomelo.EntityFrameworkCore.MySql
dotnet add src/Modules/Affiliate/Affiliate.Infrastructure/Affiliate.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design

# Shared (SHA-256 helper needs no extra package — built-in .NET)
```

### Step 9 — Build to verify
```bash
dotnet build CopyTradeMarketApi.sln
```
