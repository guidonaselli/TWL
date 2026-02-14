# Codebase Structure

**Analysis Date:** 2026-02-14

## Directory Layout

```
TheWonderlandSolution/
├── TWL.Shared/                              # Domain model (no MonoGame)
│   ├── Constants/                           # Game constants (item types, opcodes)
│   ├── Domain/                              # Core game entities
│   │   ├── Battle/                          # Combat models (ICombatant, CombatAction, StatusEffect)
│   │   ├── Characters/                      # Character types (Player, Pet, NPC, Monster)
│   │   ├── DTO/                             # Network data transfer objects
│   │   ├── Events/                          # Event definitions (battle, pet events)
│   │   ├── Graphics/                        # Visual metadata (AvatarPart, PaletteRegistry)
│   │   ├── Interactions/                    # Interaction definitions for world objects
│   │   ├── Models/                          # Core models (Item, ItemType, BindPolicy)
│   │   ├── Quests/                          # Quest definitions and rules
│   │   ├── Requests/                        # Request/response types
│   │   ├── Skills/                          # Skill definitions and system
│   │   └── World/                           # World state (maps, triggers, spawns)
│   ├── Net/                                 # Network layer (shared interfaces)
│   │   ├── Abstractions/                    # INetworkChannel, IGameManager
│   │   ├── Messages/                        # Message base classes
│   │   ├── Network/                         # Network constants and helpers
│   │   └── Payloads/                        # Message payload types
│   └── Services/                            # Shared service interfaces
│
├── TWL.Server/                              # Server (authoritative, C# console app)
│   ├── Architecture/                        # Patterns and infrastructure
│   │   ├── Observability/                   # Logging helpers (PipelineLogger)
│   │   └── Pipeline/                        # Command pattern (ICommand, ICommandHandler, IMediator, Mediator)
│   ├── Domain/                              # Server-specific domain logic
│   │   └── World/                           # Server map, trigger, spawn models
│   ├── Features/                            # Feature-specific handlers
│   │   ├── Combat/                          # Combat commands/handlers (UseSkillCommand, UseSkillHandler, TurnEngine)
│   │   └── Interactions/                    # Interaction handling (InteractCommand, InteractHandler)
│   ├── Persistence/                         # Data storage layer
│   │   ├── Database/                        # DbService (PostgreSQL via Npgsql)
│   │   └── Services/                        # PlayerService, file/DB repos
│   ├── Security/                            # Security utilities (RateLimiter, BCrypt)
│   ├── Services/                            # Domain services
│   │   └── World/                           # World management (WorldTriggerService, MapRegistry)
│   │       ├── Actions/                     # Trigger action handlers (TriggerActionRegistry, *ActionHandler)
│   │       └── Handlers/                    # Trigger evaluation handlers (TriggerHandler implementations)
│   └── Simulation/                          # Game loop and networking
│       ├── Managers/                        # Game managers (CombatManager, PetManager, MonsterManager, EconomyManager, SpawnManager)
│       ├── Networking/                      # Network code (NetworkServer, ClientSession, ServerCharacter, ServerCombatant)
│       │   └── Components/                  # Session sub-components (PlayerQuestComponent)
│       ├── Program.cs                       # Entry point (DI setup, app configuration)
│       └── ServerWorker.cs                  # IHostedService (initializes world on startup)
│
├── TWL.Client/                              # Client (MonoGame presentation layer)
│   ├── Content/                             # Game assets (sprites, fonts, audio, tiles)
│   │   ├── Assets/                          # Sprite sheets
│   │   ├── Audio/                           # Music and SFX
│   │   ├── Data/                            # Game content JSON (skills.json, quests.json, etc.)
│   │   ├── Effects/                         # MonoGame shader effects
│   │   ├── Fonts/                           # Bitmap fonts
│   │   ├── Sprites/                         # Character and object sprites
│   │   └── UI/                              # UI textures
│   ├── Prediction/                          # Client-side prediction for smooth UX
│   ├── Presentation/                        # Game presentation logic
│   │   ├── Core/                            # Game1.cs (MonoGame main loop)
│   │   ├── Crafting/                        # Crafting UI and logic
│   │   ├── Entities/                        # Entity view classes (PlayerView, NpcView)
│   │   ├── Graphics/                        # Rendering helpers
│   │   ├── Helpers/                         # Utility functions
│   │   ├── Managers/                        # Scene, asset, game, settings managers
│   │   ├── Map/                             # Map rendering (Tiled integration)
│   │   ├── Models/                          # View models for UI binding
│   │   ├── Networking/                      # Network client (LoopbackChannel, GameClientManager)
│   │   ├── Quests/                          # Quest UI and display
│   │   ├── Scenes/                          # Game scenes (SceneMainMenu, SceneGameplay, SceneBattle)
│   │   ├── Services/                        # Client-side services
│   │   ├── UI/                              # UI components and windows
│   │   └── Views/                           # View classes for rendering
│   ├── Packages/                            # Third-party content pipelines
│   ├── Resources/                           # Resource references
│   ├── Program.cs                           # DI and MonoGame setup
│   └── TWL.Client.csproj                    # Project file
│
├── TWL.Tests/                               # xUnit test project
│   ├── Architecture/                        # Mediator tests
│   ├── Benchmarks/                          # Performance benchmarks
│   ├── Characters/                          # Character model tests
│   ├── Combat/                              # Combat mechanics tests
│   ├── Content/                             # Content validation tests
│   ├── Interactions/                        # Interaction tests
│   ├── Persistence/                         # Database and save tests
│   ├── Quests/                              # Quest system tests
│   ├── Skills/                              # Skill system tests
│   └── World/                               # World simulation tests
│
├── Content/                                 # Shared content directory
│   ├── Data/                                # Game content JSON files
│   │   ├── skills.json                      # Skill definitions
│   │   ├── quests.json                      # Quest definitions
│   │   ├── pets.json                        # Pet definitions and growth models
│   │   ├── monsters.json                    # Enemy definitions
│   │   ├── items.json                       # Item definitions
│   │   ├── equipment.json                   # Equipment metadata
│   │   ├── interactions.json                # World object interactions
│   │   ├── playercolors.json                # Character color customization
│   │   └── spawns/                          # Spawn configuration files
│   └── Maps/                                # Tiled map files
│       ├── IslaBrisa/                       # Starter zone maps (0001-0099)
│       ├── PuertoRoca/                      # Hub city maps (1000-1099)
│       └── Tilesets/                        # Tileset definitions
│
├── config/                                  # Configuration files
│   └── [server configs]
│
├── docs/                                    # Design documentation
│   ├── core/                                # Core systems (architecture, localization)
│   ├── economy/                             # Economy design docs
│   ├── housing/                             # Housing system design
│   ├── maps/                                # World design docs
│   ├── pets/                                # Pet system design
│   ├── quests/                              # Quest system design
│   ├── rules/                               # Game rules and contracts
│   ├── skills/                              # Skill system design
│   └── world/                               # World state and triggers
│
├── db/                                      # Database utilities
│   └── [migration and schema files]
│
├── .planning/                               # GSD planning directory
│   └── codebase/                            # Generated architecture docs
│
├── .editorconfig                            # IDE formatting rules
├── CONTEXT.md                               # Project context and design pillars
├── CHANGELOG.md                             # Version history
├── TheWonderlandSolution.sln                # Visual Studio solution file
└── docker-compose.yml                       # PostgreSQL container config
```

## Directory Purposes

**TWL.Shared:**
- Purpose: Shared domain model; contains data structures, rules, and network contracts
- Contains: Character models, inventory, combat, quests, items, status effects, DTOs
- Key files: `Character.cs`, `PlayerCharacter.cs`, `PetCharacter.cs`, `Inventory.cs`, `Item.cs`, `Skill.cs`

**TWL.Server/Architecture/Pipeline:**
- Purpose: Implements command mediator pattern for decoupled feature handling
- Contains: `ICommand<TResult>`, `ICommandHandler<TCommand>`, `IMediator`, `Mediator` implementation
- Key files: `Pipeline/ICommand.cs`, `Pipeline/IMediator.cs`, `Pipeline/Mediator.cs`

**TWL.Server/Features:**
- Purpose: Feature-specific command handlers and logic
- Contains: Combat system (UseSkillCommand, TurnEngine), interactions (InteractCommand)
- Key files: `Combat/UseSkillHandler.cs`, `Interactions/InteractHandler.cs`

**TWL.Server/Persistence:**
- Purpose: Data access and storage layer
- Contains: Database service, player repository, entity serialization
- Key files: `Database/DbService.cs`, `FilePlayerRepository.cs`, `PlayerService.cs`

**TWL.Server/Simulation/Networking:**
- Purpose: Network communication and client session management
- Contains: TCP server, client session, message handling, server-side character models
- Key files: `NetworkServer.cs`, `ClientSession.cs`, `ServerCharacter.cs`, `ServerCombatant.cs`

**TWL.Server/Simulation/Managers:**
- Purpose: Game simulation managers (combat, pets, quests, economy, etc.)
- Contains: CombatManager, PetManager, ServerQuestManager, EconomyManager, MonsterManager, SpawnManager
- Key files: `CombatManager.cs`, `PetManager.cs`, `ServerQuestManager.cs`

**TWL.Server/Services/World:**
- Purpose: World state and trigger management
- Contains: WorldTriggerService, MapRegistry, trigger handlers, action handlers
- Key files: `WorldTriggerService.cs`, `MapRegistry.cs`, `Handlers/*`, `Actions/Handlers/*`

**TWL.Client/Presentation/Scenes:**
- Purpose: Game screens and scenes (menu, gameplay, battle)
- Contains: SceneBase, SceneMainMenu, SceneGameplay, SceneBattle
- Key files: `SceneMainMenu.cs`, `SceneGameplay.cs`, scene managers

**TWL.Client/Presentation/Entities:**
- Purpose: Visual representations of game entities
- Contains: PlayerView (sprite + equipment), NpcView, MonsterView, PetView
- Key files: `PlayerView.cs` (sprite rendering with palette swap and equipment layers)

**TWL.Client/Presentation/UI:**
- Purpose: UI windows and components
- Contains: Inventory, equipment, stats, party, quest log, chat
- Key files: UI window classes, HUD components

**Content/Data:**
- Purpose: Game content definitions
- Contains: JSON files for skills, quests, pets, items, equipment, interactions
- Key files: `skills.json`, `quests.json`, `pets.json`, `monsters.json`, `items.json`

**Content/Maps:**
- Purpose: Tiled map files for game world
- Contains: Tiled .tmx files and tileset definitions
- Key files: Maps organized by region (IslaBrisa/*, PuertoRoca/*)

## Key File Locations

**Entry Points:**
- `TWL.Server/Simulation/Program.cs`: Server initialization and DI configuration
- `TWL.Client/Program.cs`: Client initialization and MonoGame setup
- `TWL.Client/Presentation/Core/Game1.cs`: MonoGame main loop entry point

**Configuration:**
- `Persistence/ServerConfig.json`: Server port, database connection, world settings
- `Persistence/SerilogSettings.json`: Logging configuration
- `Content/Data/skills.json`: Skill system definition
- `Content/Data/quests.json`: Quest definitions
- `.editorconfig`: IDE code style rules

**Core Logic:**
- `TWL.Server/Simulation/Managers/CombatManager.cs`: Combat state and resolution
- `TWL.Server/Services/World/WorldTriggerService.cs`: World event triggers and flags
- `TWL.Server/Persistence/Database/DbService.cs`: Database layer
- `TWL.Client/Presentation/Core/Game1.cs`: Client game loop
- `TWL.Shared/Domain/Characters/PlayerCharacter.cs`: Player data model

**Testing:**
- `TWL.Tests/Combat/CombatManagerTests.cs`: Combat system tests
- `TWL.Tests/Content/ContentValidationTests.cs`: Game content validation
- `TWL.Tests/Quests/QuestSystemTests.cs`: Quest logic tests

## Naming Conventions

**Files:**
- Pascal case: `PlayerCharacter.cs`, `CombatManager.cs`, `WorldTriggerService.cs`
- Interfaces start with `I`: `ICommand.cs`, `IMediator.cs`, `ICombatResolver.cs`
- Test files end with `Tests`: `CombatManagerTests.cs`, `ContentValidationTests.cs`
- Handler files follow pattern: `{Feature}Handler.cs` (UseSkillHandler, InteractHandler)

**Directories:**
- Plural for collections: `Services/`, `Managers/`, `Handlers/`, `Features/`
- Domain areas: `Combat/`, `Characters/`, `Quests/`, `Skills/`, `World/`
- Layer names: `Domain/`, `Persistence/`, `Presentation/`, `Networking/`

**Classes:**
- Domain entities: `PlayerCharacter`, `PetCharacter`, `Inventory`, `Item`
- Managers: `CombatManager`, `PetManager`, `ServerQuestManager`, `SpawnManager`
- Services: `PlayerService`, `WorldTriggerService`, `InteractionManager`
- Handlers: `UseSkillHandler`, `InteractHandler`, `MessageActionHandler`
- Queries: `GetPlayerQuery`, `GetQuestQuery` (hypothetical future use)
- DTOs: Suffix with `DTO` or `Dto`: `PlayerDataDTO`, `MapChangeDto`

**Interfaces:**
- Contract interfaces: `ICommand<TResult>`, `ICommandHandler<TCommand>`, `IMediator`
- Service interfaces: `INetworkChannel`, `IAssetLoader`, `IGameManager`
- Repository interfaces: `IPlayerRepository`
- Factory/Registry interfaces: `IMapRegistry`, `IWorldScheduler`

**Methods:**
- Async methods: `async Task<T>` with `Async` suffix: `SendAsync()`, `HandleLoginAsync()`, `LoadAsync()`
- Event handlers: `On{Event}` pattern: `OnCombatantDeath()`, `OnFlagChanged()`, `OnMapChanged()`
- Validation: `Validate()`, `TryParse()`, `CanHandle()`
- Query: `Get*()`, `Find*()`, `TryGetValue()`: `GetCombatant()`, `FindTrigger()`, `GetSkillById()`

## Where to Add New Code

**New Skill (Feature):**
1. Add skill definition to `Content/Data/skills.json`
2. If custom logic: Create `Features/Combat/SkillHandlers/{SkillName}Handler.cs` (optional if generic)
3. Register in skill registry if needed (usually auto-loaded from JSON)
4. Add tests in `TWL.Tests/Combat/SkillSystemTests.cs`

**New Interaction (Feature):**
1. Add interaction definition to `Content/Data/interactions.json`
2. Create handler in `TWL.Server/Simulation/Managers/InteractionManager.cs` or dedicated handler
3. Register handler in `InteractionManager` constructor
4. Add tests in `TWL.Tests/Interactions/InteractionTests.cs`

**New World Trigger:**
1. Define trigger in map JSON or content configuration
2. Create trigger handler in `TWL.Server/Services/World/Handlers/{Type}Handler.cs`
3. Register handler in `WorldTriggerService.RegisterHandler()`
4. Create action handlers if needed in `Services/World/Actions/Handlers/`

**New Manager/Service:**
1. Create class in appropriate subdirectory under `Simulation/Managers/` or `Services/`
2. Implement service interface if shared with client (in `TWL.Shared/Services/`)
3. Register in DI container: `Program.cs` lines 40-111
4. Use dependency injection in dependent classes

**New Client Scene:**
1. Create class in `TWL.Client/Presentation/Scenes/` inheriting from `SceneBase`
2. Register in `SceneManager` via `RegisterScene()` call
3. Use in navigation: `_sceneManager.LoadScene("SceneName")`
4. Add assets to `Content/` directory structure

**New UI Component:**
1. Create class in `TWL.Client/Presentation/UI/` or `Views/`
2. Inherit from base UI class or implement drawing interface
3. Add to scene's UI collection: `_uiComponents.Add(newComponent)`
4. Update in scene's `Update()` and `Draw()` methods

**New Database Entity:**
1. Add table definition and migration in `db/`
2. Create repository interface in `TWL.Server/Persistence/` if needed
3. Implement repository in `FilePlayerRepository.cs` or create new repo class
4. Add DTO in `TWL.Shared/Domain/DTO/` for serialization
5. Register in DI: `Program.cs` DI section

**Utilities/Helpers:**
- Shared utilities: `TWL.Shared/Services/` or create new namespace
- Client-only helpers: `TWL.Client/Presentation/Helpers/`
- Server-only helpers: `TWL.Server/Services/` or new namespace
- Avoid top-level `Utils.cs` - organize by domain area

## Special Directories

**Content/Data:**
- Purpose: Game content definitions (JSON)
- Generated: No (hand-authored)
- Committed: Yes
- Load: Server loads on startup via `ServerQuestManager.Load()`, client loads via `AssetLoader`

**Content/Maps:**
- Purpose: Tiled map files
- Generated: No (created in Tiled editor)
- Committed: Yes
- Load: Client loads via `MonoGame.Extended.Tiled`, renders in `SceneGameplay`

**bin/ and obj/:**
- Purpose: Compiled output and intermediate build artifacts
- Generated: Yes (by dotnet build)
- Committed: No (.gitignore)

**reports/:**
- Purpose: Generated test reports and analytics
- Generated: Yes (by test runner)
- Committed: No (except documentation)

**Packages/ (TWL.Client):**
- Purpose: Third-party MonoGame content pipeline packages
- Generated: No (manually copied or NuGet managed)
- Committed: Yes (needed for build)

---

*Structure analysis: 2026-02-14*
