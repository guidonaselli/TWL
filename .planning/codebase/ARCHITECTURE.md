# Architecture

**Analysis Date:** 2026-02-14

## Pattern Overview

**Overall:** Three-tier client-server architecture with clean separation of concerns and data-driven content.

**Key Characteristics:**
- **Server-authoritative:** All game state lives on server; client renders from server data
- **Data-driven:** Game content (skills, quests, items, pets) loaded from JSON files, not hardcoded
- **Command pipeline:** Server features use mediator pattern for command handling
- **Event-driven world state:** Triggers, flags, and world events drive state transitions

## Layers

**TWL.Shared (Domain & Contracts):**
- Purpose: Pure domain model with no platform dependencies (no MonoGame, graphics, or presentation logic)
- Location: `TWL.Shared/`
- Contains: Characters, inventory, combat rules, quests, economy, network contracts (DTOs, enums)
- Depends on: .NET Framework only
- Used by: Both server and client for domain logic and serialization

**TWL.Server (Authoritative Game Server):**
- Purpose: Source of truth for all game state; validates actions, persists data, manages world simulation
- Location: `TWL.Server/`
- Contains: Networking, persistence, combat resolution, world management, feature handlers
- Depends on: TWL.Shared, PostgreSQL (Npgsql), LiteNetLib, Serilog
- Used by: Network clients connecting via TCP

**TWL.Client (MonoGame Presentation):**
- Purpose: Graphical game client; renders world, handles input, displays UI
- Location: `TWL.Client/`
- Contains: Scenes, UI views, asset loading, sprite rendering, input handling
- Depends on: TWL.Shared, MonoGame, MonoGame.Extended
- Used by: Players running the desktop application

## Data Flow

**Login Flow:**

1. Client (`Program.cs` → `Game1.cs` → `SceneMainMenu`) sends login request with credentials
2. NetworkServer accepts TCP connection, creates `ClientSession`
3. `ClientSession` authenticates via `DbService` (PostgreSQL), loads `PlayerCharacter` data
4. Server sends `PlayerDataDTO` to client containing character stats, inventory, pets, quests
5. Client creates `PlayerView` (sprite + equipment) and transitions to `SceneGameplay`

**Combat Flow:**

1. Client detects combat trigger, sends `UseSkillRequest` to server via TCP network message
2. `ClientSession.HandleMessageAsync()` receives message, creates `UseSkillCommand`
3. `IMediator.Send()` dispatches to `UseSkillHandler` (in `Features/Combat/`)
4. Handler calls `CombatManager.UseSkill()` which:
   - Validates combatants in `ITurnEngine` encounter
   - Resolves skill via `ICombatResolver` (applies damage, effects, status changes)
   - Publishes results to `OnCombatActionResolved` event
5. `ClientSession` receives event, sends `CombatResult` list back to client via `SendAsync()`
6. Client animates results and updates UI

**World Event Flow (Triggers, Flags, NPCs):**

1. Player enters map or achieves condition (damage, quest progress)
2. `WorldTriggerService.OnPlayerAction()` or `OnFlagChanged()` is called
3. Service indexes triggers by flag/location and evaluates conditions (`ITriggerCondition`)
4. Matching triggers execute actions (`ITriggerActionHandler`) - spawn mobs, teleport, give items, set flags
5. Handlers emit `WorldStateChanged` events which propagate to client

**State Management:**

- Server owns persistent state: `ServerCharacter`, `ServerPet`, `ServerCombatant` in `Simulation/Networking/`
- Client maintains volatile render state: `PlayerView`, `PlayerViewModel`, map entities
- `PlayerCharacter` and `PetCharacter` (in Shared) sync between client cache and server source
- File-based persistence via `FilePlayerRepository` and `DbService` for character saves

## Key Abstractions

**IMediator (Command Pipeline):**
- Purpose: Decouples command senders from handlers using Command pattern
- Examples: `ICommand<TResult>`, `ICommandHandler<TCommand>`
- Pattern: Used for combat (`UseSkillCommand`), interactions (`InteractCommand`)
- Location: `TWL.Server/Architecture/Pipeline/`

**ICombatResolver (Strategy Pattern):**
- Purpose: Encapsulates combat math and damage calculation
- Examples: `StandardCombatResolver` implements resolution logic
- Pattern: Injected into `CombatManager`, allows swapping damage algorithms
- Location: `TWL.Server/Simulation/Managers/`

**ITriggerHandler (Template Method):**
- Purpose: Base abstraction for world trigger execution (damage, teleport, spawn, etc.)
- Examples: `DamageTriggerHandler`, `MessageActionHandler`, `SpawnActionHandler`
- Pattern: Registry-based lookup by trigger type
- Location: `TWL.Server/Services/World/Handlers/` and `Actions/Handlers/`

**IWorldTriggerService (Facade):**
- Purpose: Central point for triggering world events, managing flags, scheduling timers
- Examples: Responds to player actions, flag changes, timer ticks
- Pattern: Maintains trigger indexes (by map, by flag) for efficient lookup
- Location: `TWL.Server/Services/World/WorldTriggerService.cs`

**INetworkChannel (Network Abstraction):**
- Purpose: Abstracts network transport for both client and server
- Examples: `NetworkServer` (TCP), `LoopbackChannel` (in-memory for testing)
- Pattern: Client sends `IClientMessage`, server sends `ServerMessage`
- Location: `TWL.Shared/Net/` and `TWL.Server/Simulation/Networking/`

## Entry Points

**Server Entry Point:**
- Location: `TWL.Server/Simulation/Program.cs`
- Triggers: dotnet run from solution root
- Responsibilities:
  - Configures DI container (all managers, services, mediator)
  - Loads configuration from `ServerConfig.json` and `SerilogSettings.json`
  - Initializes database, quest manager, skill registry
  - Starts `ServerWorker` (loads maps, starts world simulation)
  - Starts `NetworkServer` on configured port (default 5000)

**Client Entry Point:**
- Location: `TWL.Client/Program.cs`
- Triggers: dotnet run or packaged exe
- Responsibilities:
  - Configures MonoGame host with DI services
  - Registers asset loader, scene manager, game client manager
  - Creates and runs `Game1` (MonoGame main loop)
  - Initializes skill registry from `Content/Data/skills.json`

**Feature Handler Entry Points:**
- Combat: `ClientSession` parses opcode `UseSkill` → `UseSkillHandler` → `CombatManager.UseSkill()`
- Interactions: `ClientSession` parses opcode `Interact` → `InteractHandler` → `InteractionManager`
- World Triggers: `PlayerQuestComponent.OnFlagAdded` → `WorldTriggerService.OnFlagChanged()`

## Error Handling

**Strategy:** Try-catch at layer boundaries; log errors; return safe defaults (empty lists, null)

**Patterns:**

1. **Combat Validation:** `UseSkillHandler` checks combatant existence, skill validity before resolution
   - Returns empty `CombatResult` list on failure
   - Logs error via `ILogger<UseSkillHandler>`

2. **Database Operations:** `DbService` and `PlayerService` wrap Npgsql exceptions
   - Transaction rollback on Npgsql errors
   - Serilog logs to file and console

3. **Trigger Execution:** `WorldTriggerService` catches exceptions in handler execution
   - Prevents single trigger failure from stopping world simulation
   - Logs via `_logger.LogError()` with trigger ID and map ID context

4. **Network Deserialization:** `ClientSession` wraps `JsonSerializer.Deserialize()`
   - Gracefully handles malformed messages
   - Metrics incremented in `RateLimiter` for detection

## Cross-Cutting Concerns

**Logging:**
- Framework: Serilog
- Server: Console + file sink (configured via `SerilogSettings.json`)
- Client: ILogger from Microsoft.Extensions.Logging
- Pattern: Injected as `ILogger<T>` in constructors

**Validation:**
- Content validation: `TWL.Tests.ContentValidationTests` validates JSON files (skills, quests, items)
- Request validation: `QuestValidator` checks quest definitions for ID conflicts, missing refs
- Network validation: `RateLimiter` in `ClientSession` prevents abuse

**Authentication:**
- Mechanism: Username/password via `DbService` (PostgreSQL hash storage)
- Implementation: `ClientSession.HandleLoginAsync()` verifies credentials
- Protection: BCrypt.Net-Next for password hashing

**Authorization:**
- Per-operation: `ClientSession` checks owner before allowing equipment change, quest progress
- Example: `UseSkillHandler` verifies `PlayerId` in request matches authenticated session

**Performance Monitoring:**
- ServerMetrics tracks total connections, combat actions, world events
- Location: `TWL.Server/Simulation/Managers/ServerMetrics.cs`
- Usage: Referenced in `ServerWorker` and network operations
- Reports: Written to console/Serilog periodically

---

*Architecture analysis: 2026-02-14*
