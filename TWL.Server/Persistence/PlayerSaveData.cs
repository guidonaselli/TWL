using System.Collections.Generic;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Quests;
using TWL.Shared.Domain.Requests;

namespace TWL.Server.Persistence;

public class PlayerSaveData
{
    public ServerCharacterData Character { get; set; } = new();
    public QuestData Quests { get; set; } = new();
    public DateTime LastSaved { get; set; }
}

public class ServerCharacterData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Hp { get; set; }
    public int Sp { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }
    public int ExpToNextLevel { get; set; }
    public int StatPoints { get; set; }
    public int Str { get; set; }
    public int Con { get; set; }
    public int Int { get; set; }
    public int Wis { get; set; }
    public int Agi { get; set; }
    public int Gold { get; set; }
    public long PremiumCurrency { get; set; }
    public int MapId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public List<Item> Inventory { get; set; } = new();
    public List<ServerPetData> Pets { get; set; } = new();
    public List<SkillMasteryData> Skills { get; set; } = new();
    public string ActivePetInstanceId { get; set; }
}

public class SkillMasteryData
{
    public int SkillId { get; set; }
    public int Rank { get; set; }
    public int UsageCount { get; set; }
}

public class QuestData
{
    public Dictionary<int, QuestState> States { get; set; } = new();
    public Dictionary<int, List<int>> Progress { get; set; } = new();
    public HashSet<string> Flags { get; set; } = new();
    public Dictionary<int, DateTime> CompletionTimes { get; set; } = new();
    public Dictionary<int, DateTime> StartTimes { get; set; } = new();
}

public class ServerPetData
{
    public string InstanceId { get; set; }
    public int DefinitionId { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }
    public int Amity { get; set; }
    public bool IsDead { get; set; }
    public bool IsLost { get; set; }
    public bool DeathQuestCompleted { get; set; }
    public bool HasRebirthed { get; set; }
    public DateTime? ExpirationTime { get; set; }
    public List<int> UnlockedSkillIds { get; set; } = new();
}
