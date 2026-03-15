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
        CheckSkillUnlocks();
    }

    public string InstanceId { get; set; } = Guid.NewGuid().ToString();
    public int DefinitionId { get; set; }
    public int OwnerId { get; set; }

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
    /// <summary>Number of times this pet has rebirthed (0 = never). Replaces the old boolean HasRebirthed.</summary>
    public int RebirthGeneration { get; set; }

    /// <summary>True if the pet has rebirthed at least once (backward-compat helper).</summary>
    public bool HasRebirthed => RebirthGeneration > 0;
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

        if (RebirthGeneration > 0)
        {
            // Cumulative bonus: each generation adds a percentage of the BASE stat.
            // Generation 1: +10%, Gen 2: +18% (+8), Gen 3+: +23% (+5 more per gen).
            // Implemented as additive stacking per generation rather than compounding.
            float bonusMultiplier = GetCumulativeStatMultiplier(RebirthGeneration);
            _maxHp = (int)MathF.Round(_maxHp * bonusMultiplier);
            _maxSp = (int)MathF.Round(_maxSp * bonusMultiplier);
            Str = (int)MathF.Round(Str * bonusMultiplier);
            Con = (int)MathF.Round(Con * bonusMultiplier);
            Int = (int)MathF.Round(Int * bonusMultiplier);
            Wis = (int)MathF.Round(Wis * bonusMultiplier);
            Agi = (int)MathF.Round(Agi * bonusMultiplier);
        }

        float amityMultiplier = 1.0f;
        if (_definition.BondTiers != null && _definition.BondTiers.Count > 0)
        {
            // Data-driven bonding tiers
            var activeTier = _definition.BondTiers
                .Where(t => t.AmityThreshold <= Amity)
                .OrderByDescending(t => t.AmityThreshold)
                .FirstOrDefault();

            if (activeTier != null)
            {
                amityMultiplier = activeTier.StatMultiplier;
            }
        }
        else
        {
            // Legacy/Fallback behavior
            if (IsRebellious)
            {
                amityMultiplier = 0.8f;
            }
            else if (Amity >= 90)
            {
                amityMultiplier = 1.1f;
            }
        }

        if (amityMultiplier != 1.0f)
        {
            Str = (int)MathF.Round(Str * amityMultiplier);
            Con = (int)MathF.Round(Con * amityMultiplier);
            Int = (int)MathF.Round(Int * amityMultiplier);
            Wis = (int)MathF.Round(Wis * amityMultiplier);
            Agi = (int)MathF.Round(Agi * amityMultiplier);
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

    /// <summary>
    /// Returns the per-generation stat bonus as a flat percentage point (10 → 8 → 5 for gen3+).
    /// </summary>
    public static int GetRebirthBonusPoints(int generation)
    {
        return generation switch
        {
            1 => 10,
            2 => 8,
            _ => 5  // generation 3 and above
        };
    }

    /// <summary>
    /// Returns a cumulative stat multiplier for the given number of rebirth generations.
    /// Example: gen 1 → 1.10, gen 2 → 1.18, gen 3 → 1.23, gen 4 → 1.28
    /// </summary>
    public static float GetCumulativeStatMultiplier(int generations)
    {
        float totalBonus = 0f;
        for (int g = 1; g <= generations; g++)
        {
            totalBonus += GetRebirthBonusPoints(g) / 100f;
        }
        return 1f + totalBonus;
    }

    /// <summary>
    /// Attempts to rebirth this pet. Capturable pets cannot rebirth.
    /// Multi-generation rebirth is allowed for Quest/HumanLike pets using 10/8/5 diminishing schedule.
    /// </summary>
    /// <returns>True on success; false if ineligible.</returns>
    public bool TryRebirth()
    {
        // Capturable pets cannot rebirth — quest/HumanLike pets can rebirth multiple times.
        if (_definition == null)
        {
            return false;
        }

        // Eligibility: quest-eligible (non-Capture type) and explicitly marked rebirth-eligible.
        bool isCaptureType = _definition.Type == PetType.Capture;
        if (isCaptureType || !_definition.RebirthEligible)
        {
            return false;
        }

        // Eligibility check: Only Quest pets can rebirth (Must-Have)
        if (!_definition.IsQuestPet)
        {
            return false;
        }

        // Eligibility check: Only Quest pets can rebirth (Must-Have)
        if (!_definition.IsQuestPet)
        {
            return false;
        }

        if (Level < 100)
        {
            return false;
        }

        // Increment generation (supports multiple rebirths).
        RebirthGeneration++;
        Level = 1;
        Exp = 0;
        ExpToNextLevel = PetGrowthCalculator.GetExpForLevel(Level);

        RecalculateStats();
        Hp = MaxHp;
        Sp = MaxSp;

        IsDirty = true;

        // Unlock rebirth skill on first rebirth (if configured).
        if (RebirthGeneration == 1 && _definition.RebirthSkillId > 0 && !UnlockedSkillIds.Contains(_definition.RebirthSkillId))
        {
            UnlockedSkillIds.Add(_definition.RebirthSkillId);
        }

        CheckSkillUnlocks();

        return true;
    }

    public bool TryEvolve(PetDefinition nextDef)
    {
        if (nextDef == null) return false;

        // Eligibility: Only Quest pets can evolve
        if (!_definition.IsQuestPet) return false;

        _definition = nextDef;
        DefinitionId = nextDef.PetTypeId;
        Name = nextDef.Name;

        RecalculateStats();
        CheckSkillUnlocks();
        IsDirty = true;
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
            var rebirthMet = !skillSet.RequiresRebirth || RebirthGeneration > 0;

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
            RebirthGeneration = RebirthGeneration,
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
        // Migrate old saves: if HasRebirthed (legacy bool stored in data) but RebirthGeneration is 0, treat as gen 1.
        RebirthGeneration = data.RebirthGeneration > 0 ? data.RebirthGeneration : (data.HasRebirthed ? 1 : 0);
        ExpirationTime = data.ExpirationTime;
        if (data.UnlockedSkillIds != null)
        {
            UnlockedSkillIds = new List<int>(data.UnlockedSkillIds);
        }

        IsDirty = false;
    }
}