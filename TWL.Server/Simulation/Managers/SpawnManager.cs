using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TWL.Server.Domain.World;
using TWL.Shared.Domain.World;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Net.Messages;
using TWL.Shared.Net.Network;

namespace TWL.Server.Simulation.Managers;

public enum EncounterSource
{
    Random,
    Roaming,
    Scripted
}

public class SpawnManager
{
    private readonly MonsterManager _monsterManager;
    private readonly CombatManager _combatManager;
    private readonly Dictionary<int, ZoneSpawnConfig> _configs = new();
    private readonly ConcurrentDictionary<int, float> _playerSteps = new(); // PlayerId -> Steps

    private int _nextEncounterId = 1;

    public SpawnManager(MonsterManager monsterManager, CombatManager combatManager)
    {
        _monsterManager = monsterManager;
        _combatManager = combatManager;
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
        if (session.Character == null) return;

        int mapId = session.Character.MapId;
        if (mapId == 0) mapId = 1001; // Default for testing if not set

        if (!_configs.TryGetValue(mapId, out var config))
        {
            // Console.WriteLine($"No config for map {mapId}");
            return;
        }
        if (!config.RandomEncounterEnabled) return;

        int pid = session.Character.Id;
        float steps = _playerSteps.GetOrAdd(pid, 0f);

        // Use move packet delta if possible, but currently we just know "moved".
        // session.Character X/Y are updated *before* this call in ClientSession.HandleMoveAsync.
        // But we don't have "LastX/Y" easily accessible without extra state.
        // For this thin slice, we stick to packet-count but treat it as "Distance Unit" (e.g. 1 tile).
        // Since client sends MoveRequest usually per tile or small delta.
        // Refinement: Add distance check if we track previous position, but that requires Session state.

        steps += 1.0f; // Treating 1 MoveRequest as 1 Distance Unit (Tile)
        _playerSteps[pid] = steps;

        // Check chance
        if (Random.Shared.NextDouble() < config.StepChance)
        {
            // Reset steps
            _playerSteps[pid] = 0;
            StartEncounter(session, config, EncounterSource.Random);
        }
    }

    public void StartEncounter(ClientSession session, ZoneSpawnConfig config, EncounterSource source)
    {
        if (session.Character == null) return;

        // Check if already in combat
        if (_combatManager.GetCombatant(session.Character.Id) != null)
        {
            Console.WriteLine($"Player {session.Character.Id} is already in combat. Encounter ignored.");
            return;
        }

        // 1. Select Monsters
        var mobs = SelectMonsters(config, session.Character);
        if (mobs.Count == 0) return;

        // 2. Create Encounter
        int encounterId = Interlocked.Increment(ref _nextEncounterId);
        int seed = Random.Shared.Next();

        var serverMobs = new List<ServerCharacter>();
        int mobIdCounter = -1000 * encounterId; // distinct negative IDs for mobs

        for (int i = 0; i < mobs.Count; i++)
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
                Team = Team.Enemy
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
            Monsters = serverMobs.Select((m, index) => new {
                m.Id,
                m.Name,
                m.Hp,
                m.MaxHp,
                m.Level,
                m.CharacterElement,
                MonsterId = mobs[index].MonsterId,
                SpritePath = mobs[index].SpritePath
            }).ToList()
        };

        var msg = new NetMessage
        {
            Op = Opcode.EncounterStarted,
            JsonPayload = JsonSerializer.Serialize(payload)
        };

        _ = session.SendAsync(msg);
    }

    private List<MonsterDefinition> SelectMonsters(ZoneSpawnConfig config, ServerCharacter player)
    {
        var list = new List<MonsterDefinition>();

        // Gather all allowed monsters based on player position
        var candidates = new List<MonsterDefinition>();
        int totalWeight = 0;

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

        if (candidates.Count == 0) return list;

        // Pick 1-3 based on weight
        int count = Random.Shared.Next(1, 4);
        for (int i = 0; i < count; i++)
        {
            var pick = SelectWeighted(candidates, totalWeight);
            if (pick != null) list.Add(pick);
        }

        return list;
    }

    private MonsterDefinition? SelectWeighted(List<MonsterDefinition> candidates, int totalWeight)
    {
        int roll = Random.Shared.Next(0, totalWeight);
        int current = 0;
        foreach (var def in candidates)
        {
            int w = def.EncounterWeight > 0 ? def.EncounterWeight : 1;
            current += w;
            if (roll < current) return def;
        }
        return candidates.LastOrDefault();
    }

    public void Update(float dt)
    {
        // Roaming mob logic placeholder
    }
}
