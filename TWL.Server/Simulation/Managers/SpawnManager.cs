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
    private readonly ConcurrentDictionary<int, float> _playerSteps = new(); // PlayerId -> Steps
    private readonly IRandomService _random;
    private readonly PlayerService _playerService;

    private int _nextEncounterId = 1;
    private readonly List<ServerCharacter> _roamingMobs = new();
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

    public void OnPlayerMoved(ClientSession session)
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
        var steps = _playerSteps.GetOrAdd(pid, 0f);

        // Use move packet delta if possible, but currently we just know "moved".
        // session.Character X/Y are updated *before* this call in ClientSession.HandleMoveAsync.
        // But we don't have "LastX/Y" easily accessible without extra state.
        // For this thin slice, we stick to packet-count but treat it as "Distance Unit" (e.g. 1 tile).
        // Since client sends MoveRequest usually per tile or small delta.
        // Refinement: Add distance check if we track previous position, but that requires Session state.

        steps += 1.0f; // Treating 1 MoveRequest as 1 Distance Unit (Tile)
        _playerSteps[pid] = steps;

        // Check chance
        if (_random.NextDouble() < config.StepChance)
        {
            // Reset steps
            _playerSteps[pid] = 0;
            StartEncounter(session, config, EncounterSource.Random);
        }
    }

    public int StartEncounter(ClientSession session, ZoneSpawnConfig config, EncounterSource source)
    {
        if (session.Character == null)
        {
            return 0;
        }

        // Check if already in combat
        if (_combatManager.GetCombatant(session.Character.Id) != null)
        {
            Console.WriteLine($"Player {session.Character.Id} is already in combat. Encounter ignored.");
            return 0;
        }

        // 1. Select Monsters
        var mobs = SelectMonsters(config, session.Character);
        if (mobs.Count == 0)
        {
            return 0;
        }

        // 2. Create Encounter
        var encounterId = Interlocked.Increment(ref _nextEncounterId);
        var seed = _random.Next();

        var serverMobs = new List<ServerCharacter>();
        var mobIdCounter = -1000 * encounterId; // distinct negative IDs for mobs

        for (var i = 0; i < mobs.Count; i++)
        {
            var def = mobs[i];
            var mob = new ServerCharacter
            {
                Id = mobIdCounter--,
                Name = def.Name,
                Hp = def.BaseHp,
                Sp = def.BaseSp,
                Str = def.BaseStr,
                Con = def.BaseCon,
                Int = def.BaseInt,
                Wis = def.BaseWis,
                Agi = def.BaseAgi,
                CharacterElement = def.Element,
                Team = Team.Enemy,
                MonsterId = def.MonsterId,
                SpritePath = def.SpritePath
            };
            mob.SetLevel(def.Level);
            serverMobs.Add(mob);
        }

        // 3. Start in CombatManager
        var participants = new List<ServerCharacter> { session.Character };
        participants.AddRange(serverMobs);

        _combatManager.StartEncounter(encounterId, participants, seed);

        // 4. Notify Client
        var payload = new
        {
            EncounterId = encounterId,
            Source = source.ToString(),
            Seed = seed,
            Monsters = serverMobs.Select((m, index) => new
            {
                m.Id,
                m.Name,
                m.Hp,
                m.MaxHp,
                m.Level,
                m.CharacterElement,
                mobs[index].MonsterId,
                mobs[index].SpritePath
            }).ToList()
        };

        var msg = new NetMessage
        {
            Op = Opcode.EncounterStarted,
            JsonPayload = JsonSerializer.Serialize(payload)
        };

        _ = session.SendAsync(msg);
        return encounterId;
    }

    public int StartEncounter(ClientSession session, ServerCharacter roamingMob)
    {
        if (session.Character == null)
        {
            return 0;
        }

        if (_combatManager.GetCombatant(session.Character.Id) != null)
        {
            return 0;
        }

        var encounterId = Interlocked.Increment(ref _nextEncounterId);
        var seed = _random.Next();

        var serverMobs = new List<ServerCharacter> { roamingMob };

        var participants = new List<ServerCharacter> { session.Character, roamingMob };
        _combatManager.StartEncounter(encounterId, participants, seed);

        // Notify Client
        var payload = new
        {
            EncounterId = encounterId,
            Source = EncounterSource.Roaming.ToString(),
            Seed = seed,
            Monsters = serverMobs.Select((m) => new
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

        var msg = new NetMessage
        {
            Op = Opcode.EncounterStarted,
            JsonPayload = JsonSerializer.Serialize(payload)
        };

        _ = session.SendAsync(msg);
        return encounterId;
    }

    private List<MonsterDefinition> SelectMonsters(ZoneSpawnConfig config, ServerCharacter player)
    {
        var list = new List<MonsterDefinition>();

        // Gather all allowed monsters based on player position
        var candidates = new List<MonsterDefinition>();
        var totalWeight = 0;

        foreach (var region in config.SpawnRegions)
        {
            // Check bounds
            if (player.X >= region.X && player.X <= region.X + region.Width &&
                player.Y >= region.Y && player.Y <= region.Y + region.Height)
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
            }
        }

        if (candidates.Count == 0)
        {
            return list;
        }

        // Pick 1-3 based on weight
        var count = _random.Next(1, 4);
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
        var roll = _random.Next(0, totalWeight);
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
        var activeSessions = _playerService.GetAllSessions()
            .Where(s => s.Character != null && s.Character.Hp > 0)
            .ToList();

        lock (_roamingMobs)
        {
            for (var i = _roamingMobs.Count - 1; i >= 0; i--)
            {
                var mob = _roamingMobs[i];

                // Patrol Logic
                var def = _monsterManager.GetDefinition(mob.MonsterId);
                var speed = def?.Behavior?.PatrolSpeed ?? 1.0f; // Tiles per second
                var distanceToMove = speed * 32f * dt;

                // Pick a new target occasionally or if idle (simplified for now: random walk per frame with smoothing)
                // For a proper system we'd need Mob State (Idle, Patrol, Chase).
                // Here we just apply a small vector if random chance hits, but scaled by dt.

                // 2% chance per frame (at 60fps) to change direction is frequent.
                // Instead, let's just move them in a consistent random direction for a duration.
                // But without extra state, we'll stick to a simple Brownian-like motion but smoothed.

                if (_random.NextDouble() < 0.05) // 5% chance to pick a new direction
                {
                    // Store direction in a transient way if we could, but ServerCharacter doesn't have it.
                    // We'll just impulse move for now but respect speed.
                    var angle = _random.NextDouble() * Math.PI * 2;
                    var dx = Math.Cos(angle) * distanceToMove * 10; // Move for ~10 frames worth
                    var dy = Math.Sin(angle) * distanceToMove * 10;

                    mob.X += (float)dx;
                    mob.Y += (float)dy;
                }

                // Collision Check
                foreach (var session in activeSessions)
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
        var region = config.SpawnRegions[_random.Next(0, config.SpawnRegions.Count)];

        if (region.AllowedMonsterIds.Count == 0) return;
        var monsterId = region.AllowedMonsterIds[_random.Next(0, region.AllowedMonsterIds.Count)];

        var def = _monsterManager.GetDefinition(monsterId);
        if (def == null) return;

        // Create Mob
        // Note: Using negative IDs to distinguish from players
        // In a real system we might want a persistent unique ID generator or GUIDs
        // For now, simple random negative int
        var mobId = -100000 - _random.Next(0, 1000000);

        var mob = new ServerCharacter
        {
            Id = mobId,
            Name = def.Name,
            Hp = def.BaseHp,
            Sp = def.BaseSp,
            Str = def.BaseStr,
            Con = def.BaseCon,
            Int = def.BaseInt,
            Wis = def.BaseWis,
            Agi = def.BaseAgi,
            CharacterElement = def.Element,
            Team = Team.Enemy,
            MapId = mapId,
            MonsterId = monsterId,
            SpritePath = def.SpritePath,
            X = region.X * 32 + _random.Next(0, region.Width * 32),
            Y = region.Y * 32 + _random.Next(0, region.Height * 32)
        };
        mob.SetLevel(def.Level);

        lock (_roamingMobs)
        {
            _roamingMobs.Add(mob);
        }
    }
}
