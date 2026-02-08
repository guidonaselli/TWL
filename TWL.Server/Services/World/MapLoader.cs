using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using TWL.Server.Domain.World;
using TWL.Server.Domain.World.Conditions;

namespace TWL.Server.Services.World;

public class MapLoader
{
    private readonly ILogger<MapLoader> _logger;

    public MapLoader(ILogger<MapLoader> logger)
    {
        _logger = logger;
    }

    public ServerMap LoadMap(string tmxPath)
    {
        if (!File.Exists(tmxPath))
        {
            throw new FileNotFoundException($"TMX file not found: {tmxPath}");
        }

        XDocument doc;
        try
        {
            doc = XDocument.Load(tmxPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load TMX XML: {tmxPath}. Error: {ex.Message}", ex);
        }

        var root = doc.Element("map");
        if (root == null)
        {
            throw new Exception("Invalid TMX: missing <map> root element.");
        }

        var map = new ServerMap();

        // TMX attributes
        if (int.TryParse(root.Attribute("width")?.Value, out var w))
        {
            map.Width = w;
        }

        if (int.TryParse(root.Attribute("height")?.Value, out var h))
        {
            map.Height = h;
        }

        if (int.TryParse(root.Attribute("tilewidth")?.Value, out var tw))
        {
            map.TileWidth = tw;
        }

        if (int.TryParse(root.Attribute("tileheight")?.Value, out var th))
        {
            map.TileHeight = th;
        }

        // Parse ID from meta.json if exists
        var metaPath = Path.ChangeExtension(tmxPath, ".meta.json");
        if (File.Exists(metaPath))
        {
            try
            {
                var json = File.ReadAllText(metaPath);
                using var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.TryGetProperty("MapId", out var mapIdProp))
                {
                    map.Id = mapIdProp.GetInt32();
                }

                if (jsonDoc.RootElement.TryGetProperty("Name", out var nameProp))
                {
                    map.Name = nameProp.GetString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse meta.json for map {Path}", tmxPath);
            }
        }
        else
        {
            _logger.LogWarning("No meta.json found for map {Path}, MapId will be 0", tmxPath);
        }

        var objectGroups = root.Elements("objectgroup").ToList();

        // Triggers
        var triggerLayer = objectGroups.FirstOrDefault(l => l.Attribute("name")?.Value == "Triggers");
        if (triggerLayer != null)
        {
            foreach (var obj in triggerLayer.Elements("object"))
            {
                var trigger = ParseTrigger(obj);
                map.Triggers.Add(trigger);
            }
        }

        // Spawns
        var spawnLayer = objectGroups.FirstOrDefault(l => l.Attribute("name")?.Value == "Spawns");
        if (spawnLayer != null)
        {
            foreach (var obj in spawnLayer.Elements("object"))
            {
                var spawn = ParseSpawn(obj);
                map.Spawns.Add(spawn);
            }
        }

        return map;
    }

    private ServerTrigger ParseTrigger(XElement obj)
    {
        var trigger = new ServerTrigger
        {
            // TMX object ID is distinct from our logical "Id" property, but useful as fallback
            Id = obj.Attribute("id")?.Value ?? string.Empty,
            Properties = ParseProperties(obj)
        };

        if (float.TryParse(obj.Attribute("x")?.Value, out var x))
        {
            trigger.X = x;
        }

        if (float.TryParse(obj.Attribute("y")?.Value, out var y))
        {
            trigger.Y = y;
        }

        if (float.TryParse(obj.Attribute("width")?.Value, out var w))
        {
            trigger.Width = w;
        }

        if (float.TryParse(obj.Attribute("height")?.Value, out var h))
        {
            trigger.Height = h;
        }

        if (trigger.Properties.TryGetValue("TriggerType", out var type))
        {
            trigger.Type = type;
        }

        if (trigger.Properties.TryGetValue("Activation", out var activationStr) &&
            Enum.TryParse<TriggerActivationType>(activationStr, true, out var activation))
        {
            trigger.ActivationType = activation;
        }

        if (trigger.Properties.TryGetValue("Interval", out var intervalStr) &&
            int.TryParse(intervalStr, out var interval))
        {
            trigger.IntervalMs = interval;
        }

        if (trigger.Properties.TryGetValue("Cooldown", out var cooldownStr) &&
            int.TryParse(cooldownStr, out var cooldown))
        {
            trigger.CooldownMs = cooldown;
        }

        // Logical Id overrides TMX Id
        if (trigger.Properties.TryGetValue("Id", out var customId))
        {
            trigger.Id = customId;
        }

        // Parse Conditions
        if (trigger.Properties.TryGetValue("ReqLevel", out var reqLevelStr) && int.TryParse(reqLevelStr, out var reqLevel))
        {
            trigger.Conditions.Add(new LevelCondition(reqLevel));
        }

        if (trigger.Properties.TryGetValue("ReqQuestId", out var reqQuestIdStr) && int.TryParse(reqQuestIdStr, out var reqQuestId))
        {
            var reqQuestStatus = trigger.Properties.TryGetValue("ReqQuestStatus", out var status) ? status : "Completed";
            trigger.Conditions.Add(new QuestCondition(reqQuestId, reqQuestStatus));
        }

        if (trigger.Properties.TryGetValue("ReqItemId", out var reqItemIdStr) && int.TryParse(reqItemIdStr, out var reqItemId))
        {
            var reqItemCount = trigger.Properties.TryGetValue("ReqItemCount", out var countStr) && int.TryParse(countStr, out var count) ? count : 1;
            trigger.Conditions.Add(new ItemCondition(reqItemId, reqItemCount));
        }

        if (trigger.Properties.TryGetValue("ReqFlag", out var reqFlag))
        {
            var inverted = false;
            if (reqFlag.StartsWith("!"))
            {
                inverted = true;
                reqFlag = reqFlag.Substring(1);
            }
            trigger.Conditions.Add(new FlagCondition(reqFlag, inverted));
        }

        // Parse Actions
        if (trigger.Properties.TryGetValue("TeleportToMapId", out var tpMapId))
        {
            var action = new TriggerAction("Teleport", new Dictionary<string, string> { { "MapId", tpMapId } });
            if (trigger.Properties.TryGetValue("TeleportToX", out var tpX)) action.Parameters["X"] = tpX;
            if (trigger.Properties.TryGetValue("TeleportToY", out var tpY)) action.Parameters["Y"] = tpY;
            trigger.Actions.Add(action);
        }

        if (trigger.Properties.TryGetValue("SpawnMonsterId", out var spawnMobId))
        {
            var action = new TriggerAction("Spawn", new Dictionary<string, string> { { "MonsterId", spawnMobId } });
            if (trigger.Properties.TryGetValue("SpawnCount", out var spawnCount)) action.Parameters["Count"] = spawnCount;
            trigger.Actions.Add(action);
        }

        if (trigger.Properties.TryGetValue("SetFlag", out var setFlag))
        {
            trigger.Actions.Add(new TriggerAction("SetFlag", new Dictionary<string, string> { { "Flag", setFlag } }));
        }

        if (trigger.Properties.TryGetValue("RemoveFlag", out var removeFlag))
        {
            trigger.Actions.Add(new TriggerAction("RemoveFlag", new Dictionary<string, string> { { "Flag", removeFlag } }));
        }

        if (trigger.Properties.TryGetValue("GiveItemId", out var giveItemId))
        {
            var action = new TriggerAction("GiveItem", new Dictionary<string, string> { { "ItemId", giveItemId } });
            if (trigger.Properties.TryGetValue("GiveItemCount", out var giveItemCount)) action.Parameters["Count"] = giveItemCount;
            trigger.Actions.Add(action);
        }

        if (trigger.Properties.TryGetValue("HealAmount", out var healAmount))
        {
            trigger.Actions.Add(new TriggerAction("Heal", new Dictionary<string, string> { { "Amount", healAmount } }));
        }

        if (trigger.Properties.TryGetValue("DamageAmount", out var damageAmount))
        {
            trigger.Actions.Add(new TriggerAction("Damage", new Dictionary<string, string> { { "Amount", damageAmount } }));
        }

        if (trigger.Properties.TryGetValue("Message", out var msg))
        {
            trigger.Actions.Add(new TriggerAction("Message", new Dictionary<string, string> { { "Text", msg } }));
        }

        return trigger;
    }

    private ServerSpawn ParseSpawn(XElement obj)
    {
        var spawn = new ServerSpawn
        {
            Id = obj.Attribute("id")?.Value ?? string.Empty,
            Properties = ParseProperties(obj)
        };

        if (float.TryParse(obj.Attribute("x")?.Value, out var x))
        {
            spawn.X = x;
        }

        if (float.TryParse(obj.Attribute("y")?.Value, out var y))
        {
            spawn.Y = y;
        }

        if (spawn.Properties.TryGetValue("SpawnType", out var type))
        {
            spawn.Type = type;
        }

        if (spawn.Properties.TryGetValue("Id", out var customId))
        {
            spawn.Id = customId;
        }

        return spawn;
    }

    private Dictionary<string, string> ParseProperties(XElement obj)
    {
        var dict = new Dictionary<string, string>();
        var props = obj.Element("properties");
        if (props != null)
        {
            foreach (var prop in props.Elements("property"))
            {
                var name = prop.Attribute("name")?.Value;
                var value = prop.Attribute("value")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    dict[name] = value ?? string.Empty;
                }
            }
        }

        return dict;
    }
}