# External Integrations

**Analysis Date:** 2026-02-14

## APIs & External Services

**No external REST/HTTP APIs detected** - The codebase does not integrate with third-party APIs or cloud services for game functionality.

**Custom Protocol:**
- TCP-based game networking protocol (custom message format)
- Uses `System.Text.Json` for message serialization when communicating over network
- MessagePack format used for packet serialization in `TWL.Client/Presentation/Networking/LoopbackChannel.cs`

## Data Storage

**Databases:**
- PostgreSQL 16-alpine (primary data store)
  - Connection string: `Host=localhost;Port=5432;Database=twl_db;Username=twl;Password=twladmin;`
  - Client: Npgsql 10.0.1 (ADO.NET provider)
  - Initialization: `TWL.Server/Persistence/Database/DbService.cs` creates tables on startup
  - Tables: `accounts` (user credentials), `players` (character data)
  - Location: `TWL.Server/Persistence/Database/`

**File Storage:**
- Local filesystem only
- Player save data: Stored as JSON files via `TWL.Server/Persistence/FilePlayerRepository.cs`
- Game content data: JSON files in `Content/Data/` directory
  - Skills: `Content/Data/skills.json`
  - Items: `Content/Data/items.json`
  - NPCs: `Content/Data/npcs.json`
  - Monsters: `Content/Data/monsters.json`
  - Quests: `Content/Data/quests.json`
  - Pets: `Content/Data/pets.json`
  - Interactions: `Content/Data/interactions.json`
  - Spawns: `Content/Data/spawns/`

**Caching:**
- No external caching service
- In-memory caching via singleton services: `PetManager`, `MonsterManager`, `ServerQuestManager`, etc.

## Authentication & Identity

**Auth Provider:**
- Custom authentication implementation
- Password hashing: BCrypt.Net-Next 4.0.3
- Credentials stored in PostgreSQL `accounts` table
- Location: `TWL.Server/Security/` and `TWL.Server/Persistence/Database/DbService.cs`
- Flow: Username + password hash validation via `DbService.CheckLoginAsync()` method

**Session Management:**
- Custom session management via `ClientSession` class
- Sessions tied to TCP connections in `TWL.Server/Simulation/Networking/ClientSession.cs`
- Player ID extracted from login authentication

## Monitoring & Observability

**Error Tracking:**
- No external error tracking service (Sentry, Application Insights, etc.)
- In-process: Logged via Serilog to console and file

**Logs:**
- Serilog structured logging framework (version 10.0.0)
- Sinks:
  - Console output (development and runtime visibility)
  - Rolling file sink at `Logs/server-.log` with daily rotation intervals
- Configuration: `TWL.Server/Persistence/SerilogSettings.json` (MinimumLevel: Debug)
- Health check logging in `TWL.Server/Services/HealthCheckService.cs`

**Metrics:**
- Custom `ServerMetrics` class (`TWL.Server/Architecture/Observability/ServerMetrics.cs`)
- Tracks: Request counts, latency, player count, combat events
- Pipeline metrics collected in `TWL.Tests/Reliability/PipelineMetricsTests.cs`
- No integration with external metrics services (Prometheus, Grafana, etc.)

## CI/CD & Deployment

**Hosting:**
- Self-hosted console application
- Manual deployment (no CI/CD pipeline detected)
- Docker support: `docker-compose.yml` for PostgreSQL database container

**Container Orchestration:**
- Docker Compose configuration at root: `docker-compose.yml`
- Services defined: PostgreSQL 16-alpine container
- Volume mapping: `twl-postgres-data` for data persistence
- Init script: `./db/init.sql` runs on container startup
- Port mapping: 5432 (container) → 55432 (host)

**CI Pipeline:**
- No CI/CD detected (no GitHub Actions, Jenkins, Azure Pipelines, etc.)

## Environment Configuration

**Required env vars:**
- None explicitly required in code
- Database connection string: Loaded from `ServerConfig.json`
  - Can be overridden via configuration binding if environment variable `ConnectionStrings__PostgresConn` is set
- Serilog configuration: Loaded from `SerilogSettings.json`
- Optional: `Server:RandomSeed` (affects RNG behavior)
- Optional: `Economy:LedgerFile` and `Economy:ProviderSecret` (economy simulation)

**Secrets location:**
- Stored in JSON configuration files (NOT recommended for production):
  - `ServerConfig.json` contains database password
  - `SerilogSettings.json` (non-sensitive)
- No environment variable overrides detected in code
- Docker Compose: Credentials in `docker-compose.yml` environment section (dev only)

**Configuration files:**
- `ServerConfig.json` - Copied to output directory via project file
- `SerilogSettings.json` - Copied to output directory via project file
- Located in `TWL.Server/Persistence/` directory

## Webhooks & Callbacks

**Incoming:**
- No webhook endpoints
- Server accepts TCP connections from game clients only

**Outgoing:**
- No outgoing webhooks or API callbacks
- Economy ledger verification uses HMAC-SHA256 for transaction integrity checking (internal only)
  - Location: `TWL.Server/Simulation/Managers/EconomyManager.cs`
  - Purpose: Prevent tampering with economy transaction log

## Network & Message Protocol

**Game Communication:**
- Protocol: Custom binary protocol over TCP
- Message format: Custom `ServerMessage` and `ClientMessage` classes
- Serialization: System.Text.Json for JSON payloads embedded in messages
- Message types (Opcodes) defined in `TWL.Shared/Net/Network/Opcode.cs`:
  - LoginRequest, LoginResponse
  - MoveRequest
  - AttackRequest, InteractRequest, UseItemRequest
  - StartQuestRequest, ClaimRewardRequest
  - PurchaseGemsIntent, PurchaseGemsVerify, BuyShopItemRequest
  - MapChange, InventoryUpdate, StatusUpdate, and many more

**Rate Limiting:**
- Custom token bucket rate limiter in `TWL.Server/Security/RateLimiter.cs`
- Per-opcode rate limits:
  - LoginRequest: 3 requests/minute with 0.1/sec refill
  - MoveRequest: 20 burst capacity, 10/sec refill
  - AttackRequest: 10 burst, 5/sec refill
  - Economy operations: 2 requests/minute with 0.2/sec refill

---

*Integration audit: 2026-02-14*
