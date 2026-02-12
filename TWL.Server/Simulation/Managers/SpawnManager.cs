using System.Collections.Concurrent;
using System.Text.Json;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.World;
using TWL.Shared.Net.Network;
using TWL.Shared.Services;

namespace TWL.Server.Simulation.Managers;

public enum EncounterSource
{
    Random,
    Roaming,
    Scripted
}

public class SpawnManager
{
    private readonly CombatManager _combatManager;
    private readonly Dictionary<int, ZoneSpawnConfig> _configs = new();
    private readonly MonsterManager _monsterManager;
    private readonly ConcurrentDictionary<int, float> _playerSteps = new(); // PlayerId -> Accumulated Distance (Pixels)
    private readonly IRandomService _random;
    private readonly PlayerService _playerService;

    private const float TileSize = 32.0f;
    private int _nextEncounterId = 1;
    private static int _nextMobId = -1;
    private readonly List<ServerCharacter> _roamingMobs = new();
    private readonly List<ClientSession> _sessionBuffer = new();
    private float _respawnTimer;
    private const float RespawnCheckInterval = 5.0f;

    public SpawnManager(MonsterManager monsterManager, CombatManager combatManager, IRandomService random,
        PlayerService playerService)
    {
        _monsterManager = monsterManager;
        _combatManager = combatManager;
        _random = random;
        _playerService = playerService;
    }

    public void Load(string path)
    {
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Spawn definitions directory not found at {path}");
            return;
        }

        var files = Directory.GetFiles(path, "*.spawns.json", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var config = JsonSerializer.Deserialize<ZoneSpawnConfig>(json);
                if (config != null)
                {
                    // Basic validation
                    if (config.MapId <= 0)
                    {
                         Console.WriteLine($"Warning: Invalid MapId {config.MapId} in {file}");
                         continue;
                    }

                    _configs[config.MapId] = config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading spawn config {file}: {ex.Message}");
            }
        }

        Console.WriteLine($"Loaded {_configs.Count} spawn configs.");
    }

    public void OnPlayerMoved(ClientSession session, float distancePixels)
    {
        if (session.Character == null)
        {
            return;
        }

        var mapId = session.Character.MapId;
        if (mapId == 0)
        {
            mapId = 1001; // Default for testing if not set
        }

        if (!_configs.TryGetValue(mapId, out var config))
        {
            // Console.WriteLine($"No config for map {mapId}");
            return;
        }

        if (!config.RandomEncounterEnabled)
        {
            return;
        }

        var pid = session.Character.Id;
        var accumulated = _playerSteps.GetOrAdd(pid, 0f);

        accumulated += distancePixels;

        // Process full steps (tiles)
        while (accumulated >= TileSize)
        {
            accumulated -= TileSize;

            // Check chance (per full step/tile)
            if (_random.NextDouble("StepEncounterCheck") < config.StepChance)
            {
                // Reset accumulation on encounter
                accumulated = 0;
                _playerSteps[pid] = 0;
                StartEncounter(session, config, EncounterSource.Random);
                return;
            }
        }

        _playerSteps[pid] = accumulated;
    }

    // Unified Encounter Starter
    public virtual int StartEncounter(ClientSession session, List<ServerCharacter> enemies, EncounterSource source, int? seed = null)
    {
        if (session.Character == null)
        {
            return 0;
        }

        // Check if already in combat
        var existing = _combatManager.GetCombatant(session.Character.Id);
        if (existing != null)
        {
            // Idempotency: Return existing encounter ID and resend packet to handle retries/reconnects
            var existingId = existing.EncounterId;
            Console.WriteLine($"Player {session.Character.Id} is already in encounter {existingId}. Resending state.");

            // Reconstruct payload from existing combatants
            var allCombatants = _combatManager.GetAllCombatants();
            var encounterEnemies = allCombatants
                .Where(c => c.EncounterId == existingId && c.Team == Team.Enemy && c is ServerCharacter)
                .Cast<ServerCharacter>()
                .ToList();

            if (encounterEnemies.Count > 0)
            {
                 var existingPayload = new
                 {
                     EncounterId = existingId,
                     Source = source.ToString(),
                     Seed = seed ?? 0, // We don't track seed in combatant currently, but for retry 0 or placeholder is acceptable if not strictly required for sync
                     Monsters = encounterEnemies.Select(m => new
                     {
                         m.Id,
                         m.Name,
                         m.Hp,
                         m.MaxHp,
                         m.Level,
                         m.CharacterElement,
                         m.MonsterId,
                         m.SpritePath
                     }).ToList()
                 };

                 var retryMsg = new NetMessage
                 {
                     Op = Opcode.EncounterStarted,
                     JsonPayload = JsonSerializer.Serialize(existingPayload)
                 };

                 _ = session.SendAsync(retryMsg);
            }

            return existingId;
        }

        if (enemies == null || enemies.Count == 0)
        {
            return 0;
        }

        // Validation: Ensure Player is on same map as Enemies (if Enemies have valid MapId)
        // Roaming/Random mobs usually have MapId set. Scripted might not if created ad-hoc.
        // We check the first enemy as a representative.
        var firstEnemy = enemies[0];
        if (firstEnemy.MapId > 0 && firstEnemy.MapId != session.Character.MapId)
        {
            Console.WriteLine($"Error: Encounter mismatch. Player Map {session.Character.MapId} != Enemy Map {firstEnemy.MapId}");
            return 0;
        }

        // Build Participants List (including active Pet)
        var participants = new List<ServerCombatant> { session.Character };

        var pet = session.Character.GetActivePet();
        if (pet != null && !pet.IsDead && !pet.IsExpired)
        {
            participants.Add(pet);
        }

        participants.AddRange(enemies);

        // Strict Validation (Element.None allowed ONLY for QuestOnly Monsters)
        foreach (var p in participants)
        {
             if (p.CharacterElement == Element.None)
             {
                 // Check if it is a Monster (ServerCharacter with MonsterId > 0)
                 var isMob = p is ServerCharacter sc && sc.MonsterId > 0;

                 if (!isMob)
                 {
                     Console.WriteLine($"Error: Participant {p.Name} (ID: {p.Id}) has Element.None but is not a Monster. Encounter aborted.");
                     return 0;
                 }

                 // Defense in Depth: Verify QuestOnly tag
                 var mobDef = _monsterManager.GetDefinition(((ServerCharacter)p).MonsterId);
                 if (mobDef != null && !mobDef.Tags.Contains("QuestOnly"))
                 {
                     Console.WriteLine($"Error: Monster {p.Name} has Element.None but missing QuestOnly tag.");
                     return 0;
                 }
             }
        }

        var encounterId = Interlocked.Increment(ref _nextEncounterId);
        var actualSeed = seed ?? _random.Next("EncounterSeed");

        _combatManager.StartEncounter(encounterId, participants, actualSeed);

        // Notify Client
        // We map monsters to a DTO for the client
        var monsterDTOs = enemies.Select(m => new
        {
            m.Id,
            m.Name,
            m.Hp,
            m.MaxHp,
            m.Level,
            m.CharacterElement,
            m.MonsterId,
            m.SpritePath
        }).ToList();

        var payload = new
        {
            EncounterId = encounterId,
            Source = source.ToString(),
            Seed = actualSeed,
            Monsters = monsterDTOs
        };

        var msg = new NetMessage
        {
            Op = Opcode.EncounterStarted,
            JsonPayload = JsonSerializer.Serialize(payload)
        };

        _ = session.SendAsync(msg);
        return encounterId;
    }

    public int StartEncounter(ClientSession session, ZoneSpawnConfig config, EncounterSource source)
    {
        // 1. Select Monsters
        var mobs = SelectMonsters(config, session.Character);
        if (mobs.Count == 0)
        {
            return 0;
        }

        // 2. Instantiate ServerCharacters
        var serverMobs = new List<ServerCharacter>();
        // Using a temporary negative ID generator relative to timestamp or just random to avoid collisions in short term
        // Ideally CombatManager assigns runtime IDs. But we need them before.
        // Let's use a simple counter mechanism for the mob batch.

        foreach (var def in mobs)
        {
             var mob = CreateMobInstance(def, config.MapId, 0, 0); // Pos 0,0 for random encounters (abstract)
             serverMobs.Add(mob);
        }

        return StartEncounter(session, serverMobs, source);
    }

    public int StartEncounter(ClientSession session, ServerCharacter roamingMob)
    {
        var serverMobs = new List<ServerCharacter> { roamingMob };
        return StartEncounter(session, serverMobs, EncounterSource.Roaming);
    }

    public virtual int StartScriptedEncounter(ClientSession session, int monsterId, int count)
    {
        var def = _monsterManager.GetDefinition(monsterId);
        if (def == null)
        {
            return 0;
        }

        var mobs = new List<ServerCharacter>();
        for (int i = 0; i < count; i++)
        {
            // Use player position/map for the mob context
            var x = session.Character?.X ?? 0;
            var y = session.Character?.Y ?? 0;
            var mapId = session.Character?.MapId ?? 0;

            var mob = CreateMobInstance(def, mapId, x, y);
            mobs.Add(mob);
        }

        return StartEncounter(session, mobs, EncounterSource.Scripted);
    }

    private List<MonsterDefinition> SelectMonsters(ZoneSpawnConfig config, ServerCharacter player)
    {
        var list = new List<MonsterDefinition>();

        // Gather all allowed monsters based on player position
        var candidates = new List<MonsterDefinition>();
        var totalWeight = 0;

        // Player Position is in Pixels. Regions are in Tiles.
        var pTileX = player.X / 32f;
        var pTileY = player.Y / 32f;

        foreach (var region in config.SpawnRegions)
        {
            // Check bounds (Region X/Y/Width/Height are in Tiles)
            if (pTileX >= region.X && pTileX <= region.X + region.Width &&
                pTileY >= region.Y && pTileY <= region.Y + region.Height)
            {
                foreach (var mid in region.AllowedMonsterIds)
                {
                    var def = _monsterManager.GetDefinition(mid);
                    if (def != null)
                    {
                        candidates.Add(def);
                        totalWeight += def.EncounterWeight > 0 ? def.EncounterWeight : 1;
                    }
                }

                if (region.AllowedFamilyIds.Count > 0)
                {
                    var familyMobs = _monsterManager.GetAllDefinitions()
                        .Where(m => region.AllowedFamilyIds.Contains(m.FamilyId));
                    foreach (var def in familyMobs)
                    {
                        if (def != null)
                        {
                            candidates.Add(def);
                            totalWeight += def.EncounterWeight > 0 ? def.EncounterWeight : 1;
                        }
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            return list;
        }

        // Pick 1-3 based on weight
        var count = _random.Next(1, 4, "MobCount");
        for (var i = 0; i < count; i++)
        {
            var pick = SelectWeighted(candidates, totalWeight);
            if (pick != null)
            {
                list.Add(pick);
            }
        }

        return list;
    }

    private MonsterDefinition? SelectWeighted(List<MonsterDefinition> candidates, int totalWeight)
    {
        var roll = _random.Next(0, totalWeight, "MobSelection");
        var current = 0;
        foreach (var def in candidates)
        {
            var w = def.EncounterWeight > 0 ? def.EncounterWeight : 1;
            current += w;
            if (roll < current)
            {
                return def;
            }
        }

        return candidates.LastOrDefault();
    }

    public void Update(float dt)
    {
        _respawnTimer += dt;
        if (_respawnTimer >= RespawnCheckInterval)
        {
            _respawnTimer = 0;
            CheckRespawns();
        }

        UpdateRoamingMobs(dt);
    }

    private void UpdateRoamingMobs(float dt)
    {
        _playerService.GetSessions(_sessionBuffer, s => s.Character != null && s.Character.Hp > 0);

        lock (_roamingMobs)
        {
            for (var i = _roamingMobs.Count - 1; i >= 0; i--)
            {
                var mob = _roamingMobs[i];

                // Patrol Logic
                var def = _monsterManager.GetDefinition(mob.MonsterId);
                var speed = def?.Behavior?.PatrolSpeed ?? 1.0f; // Tiles per second
                var distanceToMove = speed * 32f * dt;

                // Simple Brownian motion
                // 5% chance per frame to change direction is too high for 60fps.
                // Assuming Update is called frequently. Let's make it 2% chance.

                if (_random.NextDouble("RoamingDirectionCheck") < 0.02)
                {
                    var angle = _random.NextDouble("RoamingAngle") * Math.PI * 2;
                    // Move for 1 sec worth of distance in this direction (simulated impulse)
                    var dx = Math.Cos(angle) * distanceToMove * 30; // 30 frames worth
                    var dy = Math.Sin(angle) * distanceToMove * 30;

                    // Apply immediately (simplified)
                    mob.X += (float)dx;
                    mob.Y += (float)dy;
                }

                // Keep within map bounds?
                // We don't have map bounds easily accessible here without loading map data.
                // For now, let them roam freely.

                // Collision Check
                foreach (var session in _sessionBuffer)
                {
                    if (session.Character.MapId == mob.MapId)
                    {
                        var distSq = Microsoft.Xna.Framework.Vector2.DistanceSquared(
                            new Microsoft.Xna.Framework.Vector2(session.Character.X, session.Character.Y),
                            new Microsoft.Xna.Framework.Vector2(mob.X, mob.Y)
                        );

                        // 32px threshold -> 1024 sq
                        if (distSq < 1024)
                        {
                            StartEncounter(session, mob);
                            _roamingMobs.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }
        _sessionBuffer.Clear();
    }

    private void CheckRespawns()
    {
        foreach (var kvp in _configs)
        {
            var mapId = kvp.Key;
            var config = kvp.Value;

            var count = 0;
            lock (_roamingMobs)
            {
                count = _roamingMobs.Count(m => m.MapId == mapId);
            }

            if (count < config.MinMobCount)
            {
                var needed = config.MinMobCount - count;
                // Limit spawn per tick to avoid spikes
                var toSpawn = Math.Min(needed, 5);
                for (var i = 0; i < toSpawn; i++)
                {
                    SpawnMob(mapId, config);
                }
            }
        }
    }

    private void SpawnMob(int mapId, ZoneSpawnConfig config)
    {
        if (config.SpawnRegions.Count == 0) return;
        var region = config.SpawnRegions[_random.Next(0, config.SpawnRegions.Count, "SpawnRegionSelect")];

        if (region.AllowedMonsterIds.Count == 0) return;
        var monsterId = region.AllowedMonsterIds[_random.Next(0, region.AllowedMonsterIds.Count, "SpawnMobSelect")];

        var def = _monsterManager.GetDefinition(monsterId);
        if (def == null) return;

        // Position
        var tileX = region.X + _random.NextDouble("SpawnPositionX") * region.Width;
        var tileY = region.Y + _random.NextDouble("SpawnPositionY") * region.Height;

        var mob = CreateMobInstance(def, mapId, (float)tileX * 32f, (float)tileY * 32f);

        lock (_roamingMobs)
        {
            _roamingMobs.Add(mob);
        }
    }

    private ServerCharacter CreateMobInstance(MonsterDefinition def, int mapId, float x, float y)
    {
        // Generate a unique temporary negative ID
        var mobId = Interlocked.Decrement(ref _nextMobId);

        var mob = new ServerCharacter
        {
            Id = mobId,
            Name = def.Name,
            Str = def.BaseStr,
            Con = def.BaseCon,
            Int = def.BaseInt,
            Wis = def.BaseWis,
            Agi = def.BaseAgi,
            CharacterElement = def.Element,
            Team = Team.Enemy,
            MapId = mapId,
            MonsterId = def.MonsterId,
            SpritePath = def.SpritePath,
            X = x,
            Y = y
        };
        mob.SetLevel(def.Level);
        mob.SetOverrideStats(def.BaseHp, def.BaseSp);
        return mob;
    }
}
