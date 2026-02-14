# Technology Stack

**Analysis Date:** 2026-02-14

## Languages

**Primary:**
- C# 12+ (latest language version) - Used across all projects

## Runtime

**Environment:**
- .NET 10.0 (target framework defined in `Directory.Build.props`)
- Supports both .NET 10.0 and .NET 8.0 (parallel build configuration in `bin/Debug` directories)

**Package Manager:**
- NuGet
- Lockfile: packages.lock.json (auto-generated)

## Frameworks

**Core:**
- Microsoft.Extensions.DependencyInjection 10.0.2 - Service container and IoC configuration
- Microsoft.Extensions.Configuration 10.0.2 - Configuration management
- Microsoft.Extensions.Logging 10.0.2 - Logging abstraction
- Microsoft.Extensions.Hosting.Abstractions 10.0.2 - Host abstraction

**Client (Game):**
- MonoGame.Framework.DesktopGL 3.8.4.1 - 2D game rendering engine
- MonoGame.Extended 5.3.1 - MonoGame extensions for utilities
- Microsoft.Xna.Framework - XNA game development framework API

**Testing:**
- xunit 2.9.3 - Test framework runner
- xunit.runner.visualstudio 3.1.5 - Visual Studio test explorer integration
- Moq 4.18.4 - Mocking framework
- Microsoft.NET.Test.Sdk 18.0.1 - Test SDK
- coverlet.collector 6.0.4 - Code coverage collection

**Logging & Observability:**
- Serilog.AspNetCore 10.0.0 - Structured logging framework
- Serilog.Sinks.Console 6.1.1 - Console output sink
- Serilog.Sinks.File 7.0.0 - File output sink with rolling intervals

**Build/Dev:**
- MonoGame.Content.Builder.Task - MGCB content pipeline (referenced via Packages directory)
- MonoGame.Extended.Content.Pipeline - Extended content pipeline support

## Key Dependencies

**Critical:**
- Npgsql 10.0.1 - PostgreSQL ADO.NET data provider; used in `TWL.Server/Persistence/Database/DbService.cs` for database connection and query execution
- LiteNetLib 1.3.5 - Lightweight UDP networking library (referenced in `TWL.Server.csproj` but implementation uses TCP/TcpListener instead)
- BCrypt.Net-Next 4.0.3 - Password hashing for authentication in `TWL.Server/Security/`
- MessagePack 3.1.4 - Binary serialization format used in `TWL.Client/Presentation/Networking/LoopbackChannel.cs` for packet serialization

**Infrastructure:**
- System.Text.Json - Built-in JSON serialization (used throughout for configuration and data serialization)

## Configuration

**Environment:**
- Configuration sources (in priority order):
  1. `TWL.Server/Persistence/ServerConfig.json` - Server network and database configuration
  2. `TWL.Server/Persistence/SerilogSettings.json` - Logging configuration
  3. appsettings.json pattern (via Microsoft.Extensions.Configuration)

**Key configurations:**
- `ServerConfig.json`: Contains `ConnectionStrings.PostgresConn` for database access and `Network.Port` for server listen port (7777)
- `SerilogSettings.json`: Configures Serilog with console and file sinks; logs to `Logs/server-.log` with daily rolling intervals

**Build:**
- `.editorconfig` - Code style and formatting rules
- `Directory.Build.props` - Central property definitions:
  - TargetFramework: net10.0
  - ImplicitUsings: enabled
  - Nullable: enabled (strict null checking)
  - LangVersion: latest
  - EnforceCodeStyleInBuild: true
  - AnalysisLevel: latest-recommended

## Platform Requirements

**Development:**
- Rider IDE (suggested by .idea directory)
- .NET 10.0 SDK or .NET 8.0 SDK
- PostgreSQL 16-alpine (via Docker container)

**Production:**
- Self-hosted server application (console application via `dotnet run`)
- PostgreSQL 16 database
- Port 7777 (TCP) for game server networking
- Port 5432 (TCP) for PostgreSQL (via docker-compose: mapped to 55432)

**Content Pipeline:**
- MonoGame Content Builder (MGCB) for asset compilation
- Pre-compiled content assets stored in `TWL.Client/Content/bin/DesktopGL/Content/`
- MGCB pipeline skipped for net10.0 builds (`MonoGameContentBuilderSkip: true`)

---

*Stack analysis: 2026-02-14*
