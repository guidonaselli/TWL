using TWL.Server.Persistence;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Characters;

namespace TWL.Server.Simulation.Networking;

public class ServerPet : ServerCombatant
{
    // Transient
    private PetDefinition _definition;

    private int _maxHp;
    private int _maxSp;

    public ServerPet()
    {
    }

    public ServerPet(PetDefinition def)
    {
        DefinitionId = def.PetTypeId;
        Name = def.Name;
        CharacterElement = def.Element;

        Hydrate(def);

        Level = 1;
        Exp = 0;
        Amity = 50;

        if (def.IsTemporary && def.DurationSeconds.HasValue)
        {
            ExpirationTime = DateTime.UtcNow.AddSeconds(def.DurationSeconds.Value);
        }

        RecalculateStats();
        Hp = MaxHp;
        Sp = MaxSp;
        ExpToNextLevel = PetGrowthCalculator.GetExpForLevel(Level);
    }

    public string InstanceId { get; set; } = Guid.NewGuid().ToString();
    public int DefinitionId { get; set; }

    // ServerCombatant has Name, Id, Hp, Sp, Stats, StatusEffects

    // Runtime ID for Combat (separate from InstanceId which is persistent GUID)
    // ServerCombatant.Id is used as the Combat ID.
    // It should be assigned when entering combat or session load.

    public int Level { get; set; } = 1;
    public int Exp { get; set; }
    public int ExpToNextLevel { get; set; } = 100;

    public int Amity { get; set; } = 50;

    public bool IsDead { get; set; }
    public bool IsLost { get; set; }
    public bool DeathQuestCompleted { get; set; }
    public bool HasRebirthed { get; set; }
    public DateTime? ExpirationTime { get; set; }

    public bool IsExpired => ExpirationTime.HasValue && DateTime.UtcNow > ExpirationTime.Value;

    public List<int> UnlockedSkillIds { get; private set; } = new();
    public bool IsRebellious => Amity < 20;

    public override int MaxHp => _maxHp;
    public override int MaxSp => _maxSp;

    public void Hydrate(PetDefinition def)
    {
        if (def.Element == Element.None)
        {
            throw new InvalidOperationException($"Pet {def.PetTypeId} ({def.Name}) has Element.None which is forbidden.");
        }

        _definition = def;
        CharacterElement = def.Element;
        // Ensure Name is set if missing (e.g. from old save)
        if (string.IsNullOrEmpty(Name))
        {
            Name = def.Name;
        }

        RecalculateStats();
    }

    public void SetDefinition(PetDefinition def) => Hydrate(def);

    public void AddExp(int amount)
    {
        if (_definition == null)
        {
            return;
        }

        Exp += amount;
        IsDirty = true;
        var leveledUp = false;

        while (Exp >= ExpToNextLevel)
        {
            Exp -= ExpToNextLevel;
            Level++;
            leveledUp = true;
            ExpToNextLevel = PetGrowthCalculator.GetExpForLevel(Level);
        }

        if (leveledUp)
        {
            RecalculateStats();
            Hp = MaxHp;
            Sp = MaxSp;
            CheckSkillUnlocks();
        }
    }

    public void SetLevel(int level)
    {
        Level = level;
        Exp = 0;
        ExpToNextLevel = PetGrowthCalculator.GetExpForLevel(Level);

        RecalculateStats();
        Hp = MaxHp;
        Sp = MaxSp;

        CheckSkillUnlocks();
        IsDirty = true;
    }

    public void RecalculateStats()
    {
        if (_definition == null)
        {
            return;
        }

        PetGrowthCalculator.CalculateStats(_definition, Level,
            out var maxHp, out var maxSp,
            out var str, out var con, out var int_, out var wis, out var agi);

        _maxHp = maxHp;
        _maxSp = maxSp;
        Str = str;
        Con = con;
        Int = int_;
        Wis = wis;
        Agi = agi;

        if (HasRebirthed)
        {
            _maxHp = (int)(_maxHp * 1.1);
            _maxSp = (int)(_maxSp * 1.1);
            Str = (int)(Str * 1.1);
            Con = (int)(Con * 1.1);
            Int = (int)(Int * 1.1);
            Wis = (int)(Wis * 1.1);
            Agi = (int)(Agi * 1.1);
        }

        if (IsRebellious)
        {
            // Amity < 20 Penalty: -20% Stats
            Str = (int)(Str * 0.8);
            Con = (int)(Con * 0.8);
            Int = (int)(Int * 0.8);
            Wis = (int)(Wis * 0.8);
            Agi = (int)(Agi * 0.8);
        }
        else if (Amity >= 90)
        {
            // High Amity Bonus: +10% Stats
            Str = (int)(Str * 1.1);
            Con = (int)(Con * 1.1);
            Int = (int)(Int * 1.1);
            Wis = (int)(Wis * 1.1);
            Agi = (int)(Agi * 1.1);
        }
    }

    public bool CheckObedience(float roll)
    {
        if (Amity >= 60)
        {
            return true; // Always obeys
        }

        if (Amity >= 20)
        {
            return true; // Normal range
        }

        // Amity < 20 (Rebellious)
        // Chance to disobey increases as Amity drops.
        // Amity 19 -> 5% fail
        // Amity 0 -> 24% fail
        var failChance = (20 - Amity) * 0.01f + 0.04f;
        return roll > failChance;
    }

    public void ChangeAmity(int amount)
    {
        var oldAmity = Amity;
        var wasRebellious = IsRebellious;

        Amity = Math.Clamp(Amity + amount, 0, 100);

        if (oldAmity != Amity)
        {
            IsDirty = true;
            CheckSkillUnlocks();

            if (wasRebellious != IsRebellious)
            {
                RecalculateStats();
            }
        }
    }

    public void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        Hp = 0;
        Sp = 0;

        ChangeAmity(-1);

        IsDirty = true;
    }

    public void Revive(int? hpOverride = null)
    {
        if (!IsDead)
        {
            return;
        }

        IsDead = false;
        Hp = hpOverride.HasValue ? Math.Min(hpOverride.Value, MaxHp) : MaxHp;
        Sp = MaxSp;

        IsDirty = true;
    }

    public bool TryRebirth()
    {
        if (_definition == null || !(_definition.RebirthEligible || _definition.RebirthSkillId > 0))
        {
            return false;
        }

        if (Level < 100)
        {
            return false;
        }

        if (HasRebirthed)
        {
            return false;
        }

        HasRebirthed = true;
        Level = 1;
        Exp = 0;
        ExpToNextLevel = PetGrowthCalculator.GetExpForLevel(Level);

        RecalculateStats();
        Hp = MaxHp;
        Sp = MaxSp;

        IsDirty = true;

        if (_definition.RebirthSkillId > 0 && !UnlockedSkillIds.Contains(_definition.RebirthSkillId))
        {
            UnlockedSkillIds.Add(_definition.RebirthSkillId);
        }

        CheckSkillUnlocks();

        return true;
    }

    public void CheckSkillUnlocks()
    {
        if (_definition == null)
        {
            return;
        }

        foreach (var skillSet in _definition.SkillSet)
        {
            if (UnlockedSkillIds.Contains(skillSet.SkillId))
            {
                continue;
            }

            var levelMet = Level >= skillSet.UnlockLevel;
            var amityMet = Amity >= skillSet.UnlockAmity;
            var rebirthMet = !skillSet.RequiresRebirth || HasRebirthed;

            if (levelMet && amityMet && rebirthMet)
            {
                UnlockedSkillIds.Add(skillSet.SkillId);
                // Also add to Combatant SkillMastery for use
                IncrementSkillUsage(skillSet.SkillId); // Initialize mastery
            }
        }
    }

    public override void ReplaceSkill(int oldId, int newId)
    {
        // For pets, this might just swap the ID in UnlockedSkillIds if we wanted to support it
        if (UnlockedSkillIds.Contains(oldId))
        {
            UnlockedSkillIds.Remove(oldId);
            UnlockedSkillIds.Add(newId);

            // Sync with SkillMastery if needed
            // But SkillMastery is a Dictionary<int, SkillMastery>, so we don't need to "remove" unless we want to clear history
        }

        IsDirty = true;
    }

    public float GetUtilityValue(PetUtilityType type)
    {
        if (_definition == null || _definition.Utilities == null)
        {
            return 0f;
        }

        var utility = _definition.Utilities.FirstOrDefault(u => u.Type == type);
        if (utility == null)
        {
            return 0f;
        }

        if (Level < utility.RequiredLevel || Amity < utility.RequiredAmity)
        {
            return 0f;
        }

        return utility.Value;
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
            ExpirationTime = ExpirationTime,
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
        ExpirationTime = data.ExpirationTime;
        if (data.UnlockedSkillIds != null)
        {
            UnlockedSkillIds = new List<int>(data.UnlockedSkillIds);
        }

        IsDirty = false;
    }
}