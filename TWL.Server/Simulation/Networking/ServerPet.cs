using System;
using System.Collections.Generic;
using System.Linq;
using TWL.Server.Persistence;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Characters;

namespace TWL.Server.Simulation.Networking;

public class ServerPet
{
    public string InstanceId { get; set; } = Guid.NewGuid().ToString();
    public int DefinitionId { get; set; }
    public string Name { get; set; }

    public bool IsDirty { get; set; }

    public int Level { get; set; } = 1;
    public int Exp { get; set; }
    public int ExpToNextLevel { get; set; } = 100;

    public int Amity { get; set; } = 50;

    public bool IsDead { get; set; }
    public bool IsLost { get; set; }
    public bool DeathQuestCompleted { get; set; }
    public bool HasRebirthed { get; set; }

    // Stats
    public int Str { get; private set; }
    public int Con { get; private set; }
    public int Int { get; private set; }
    public int Wis { get; private set; }
    public int Agi { get; private set; }

    public int Hp { get; set; }
    public int Sp { get; set; }
    public int MaxHp { get; private set; }
    public int MaxSp { get; private set; }

    // Derived Battle Stats (Simplified for now, can be expanded)
    public int Atk => (Str * 2);
    public int Def => (Con * 2);
    public int Mat => (Int * 2);
    public int Mdf => (Wis * 2);
    public int Spd => Agi;

    public List<int> UnlockedSkillIds { get; private set; } = new();

    // Transient
    private PetDefinition _definition;
    public bool IsRebellious => Amity < 20; // Example threshold

    public ServerPet() { }

    public ServerPet(PetDefinition def)
    {
        DefinitionId = def.PetTypeId;
        Name = def.Name;
        _definition = def;

        Level = 1;
        Exp = 0;
        Amity = 50; // Default amity

        RecalculateStats();
        Hp = MaxHp;
        Sp = MaxSp;
        ExpToNextLevel = PetGrowthCalculator.GetExpForLevel(Level);
    }

    public void SetDefinition(PetDefinition def)
    {
        _definition = def;
        // Verify stats match definition/level, if not recalcing, at least update max stats
        RecalculateStats();
    }

    public void AddExp(int amount)
    {
        if (_definition == null) return; // Cannot grow without definition

        Exp += amount;
        IsDirty = true;
        bool leveledUp = false;

        while (Exp >= ExpToNextLevel)
        {
            Exp -= ExpToNextLevel;
            Level++;
            leveledUp = true;

            // Recalculate Exp Requirement
            ExpToNextLevel = PetGrowthCalculator.GetExpForLevel(Level);
        }

        if (leveledUp)
        {
            RecalculateStats();
            // Heal on level up? Usually full heal.
            Hp = MaxHp;
            Sp = MaxSp;
            CheckSkillUnlocks();
        }
    }

    public void RecalculateStats()
    {
        if (_definition == null) return;

        PetGrowthCalculator.CalculateStats(_definition, Level,
            out int maxHp, out int maxSp,
            out int str, out int con, out int int_, out int wis, out int agi);

        MaxHp = maxHp;
        MaxSp = maxSp;
        Str = str;
        Con = con;
        Int = int_;
        Wis = wis;
        Agi = agi;

        // Apply Rebirth Bonus if implemented
        if (HasRebirthed)
        {
            // Simple bonus: +10% all stats
            MaxHp = (int)(MaxHp * 1.1);
            MaxSp = (int)(MaxSp * 1.1);
            Str = (int)(Str * 1.1);
            Con = (int)(Con * 1.1);
            Int = (int)(Int * 1.1);
            Wis = (int)(Wis * 1.1);
            Agi = (int)(Agi * 1.1);
        }
    }

    public void ChangeAmity(int amount)
    {
        int oldAmity = Amity;
        Amity = Math.Clamp(Amity + amount, 0, 100);

        if (oldAmity != Amity)
        {
            IsDirty = true;
            CheckSkillUnlocks();
        }
    }

    public void Die()
    {
        if (IsDead) return;

        IsDead = true;
        Hp = 0;
        Sp = 0;

        // Amity Penalty
        ChangeAmity(-10);

        IsDirty = true;
    }

    public void Revive()
    {
        if (!IsDead) return;

        IsDead = false;
        Hp = MaxHp;
        Sp = MaxSp;

        IsDirty = true;
    }

    public bool TryRebirth()
    {
        if (_definition == null || !(_definition.RebirthEligible || _definition.RebirthSkillId > 0))
            return false;

        if (Level < 100) return false; // Hardcoded req for now
        if (HasRebirthed) return false; // One rebirth limit for now?

        HasRebirthed = true;
        Level = 1;
        Exp = 0;
        ExpToNextLevel = PetGrowthCalculator.GetExpForLevel(Level);

        // Rebirth resets Amity? Usually no.

        RecalculateStats();
        Hp = MaxHp;
        Sp = MaxSp;

        IsDirty = true;

        // Unlock Rebirth Skill
        if (_definition.RebirthSkillId > 0 && !UnlockedSkillIds.Contains(_definition.RebirthSkillId))
        {
            UnlockedSkillIds.Add(_definition.RebirthSkillId);
        }

        return true;
    }

    public void CheckSkillUnlocks()
    {
        if (_definition == null) return;

        foreach (var skillSet in _definition.SkillSet)
        {
            if (UnlockedSkillIds.Contains(skillSet.SkillId)) continue;

            bool levelMet = Level >= skillSet.UnlockLevel;
            bool amityMet = Amity >= skillSet.UnlockAmity;
            bool rebirthMet = !skillSet.RequiresRebirth || HasRebirthed;

            if (levelMet && amityMet && rebirthMet)
            {
                UnlockedSkillIds.Add(skillSet.SkillId);
            }
        }
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
            HasRebirthed = HasRebirthed,
            UnlockedSkillIds = new List<int>(UnlockedSkillIds)
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
        if (data.UnlockedSkillIds != null)
        {
            UnlockedSkillIds = new List<int>(data.UnlockedSkillIds);
        }

        IsDirty = false;

        // Definition must be set externally to calc stats
    }
}
