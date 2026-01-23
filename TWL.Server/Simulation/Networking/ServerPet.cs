using System;
using TWL.Server.Persistence;
using TWL.Shared.Domain.Characters;

namespace TWL.Server.Simulation.Networking;

public class ServerPet
{
    public string InstanceId { get; set; } = Guid.NewGuid().ToString();
    public int DefinitionId { get; set; }
    public string Name { get; set; }

    public int Level { get; set; } = 1;
    public int Exp { get; set; }
    public int ExpToNextLevel { get; set; } = 100;

    public int Amity { get; set; } = 50;

    public bool IsDead { get; set; }
    public bool IsLost { get; set; }
    public bool DeathQuestCompleted { get; set; }
    public bool HasRebirthed { get; set; }

    public int Hp { get; set; }
    public int Sp { get; set; }
    public int MaxHp { get; set; }
    public int MaxSp { get; set; }

    public ServerPet() { }

    public ServerPet(PetDefinition def)
    {
        DefinitionId = def.PetTypeId;
        Name = def.Name;
        Level = 1;
        Exp = 0;
        ExpToNextLevel = 100;
        Amity = 50;

        MaxHp = def.BaseHp;
        Hp = MaxHp;
    }

    public void AddExp(int amount)
    {
        Exp += amount;
        while (Exp >= ExpToNextLevel)
        {
            Exp -= ExpToNextLevel;
            Level++;
            ExpToNextLevel = (int)(ExpToNextLevel * 1.25);
            MaxHp += 5;
            Hp = MaxHp;
        }
    }

    public void ChangeAmity(int amount)
    {
        Amity = Math.Clamp(Amity + amount, 0, 100);
    }

    public ServerPetData GetSaveData()
    {
        return new ServerPetData
        {
            InstanceId = InstanceId,
            DefinitionId = DefinitionId,
            Name = Name,
            Level = Level,
            Exp = Exp,
            Amity = Amity,
            IsDead = IsDead,
            IsLost = IsLost,
            DeathQuestCompleted = DeathQuestCompleted,
            HasRebirthed = HasRebirthed
        };
    }

    public void LoadSaveData(ServerPetData data)
    {
        InstanceId = data.InstanceId;
        DefinitionId = data.DefinitionId;
        Name = data.Name;
        Level = data.Level;
        Exp = data.Exp;
        Amity = data.Amity;
        IsDead = data.IsDead;
        IsLost = data.IsLost;
        DeathQuestCompleted = data.DeathQuestCompleted;
        HasRebirthed = data.HasRebirthed;
    }
}
