using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;
using TWL.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TWL.Tests.PetTests;

public class PetCombatAiTests
{
    private readonly AutoBattleManager _autoBattle;
    private readonly PetBattlePolicy _policy;
    private readonly Mock<ISkillCatalog> _mockSkills;
    private readonly Mock<IRandomService> _mockRandom;

    public PetCombatAiTests()
    {
        _mockSkills = new Mock<ISkillCatalog>();
        _mockRandom = new Mock<IRandomService>();
        _autoBattle = new AutoBattleManager(_mockSkills.Object);
        _policy = new PetBattlePolicy(_autoBattle, NullLogger<PetBattlePolicy>.Instance);
    }

    [Fact]
    public void PetAI_PrioritizesHealing_WhenAllyIsLow()
    {
        // Setup
        var pet = CreatePet(1, Team.Player, Element.Earth);
        pet.Int = 20; // MaxSp = 100
        pet.Sp = 100;

        var lowHpAlly = new ServerCharacter { Id = 2, Team = Team.Player, Con = 10, Hp = 20 }; // MaxHp = 100
        var enemy = new ServerCharacter { Id = 3, Team = Team.Enemy, Con = 10, Hp = 100 };

        var participants = new List<ServerCombatant> { pet, lowHpAlly, enemy };

        // Define a heal skill
        var healSkill = new Skill
        {
            SkillId = 10,
            Name = "Heal",
            SpCost = 10,
            TargetType = SkillTargetType.SingleAlly,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Heal, Value = 50, Chance = 1.0f } }
        };
        _mockSkills.Setup(s => s.GetSkillById(10)).Returns(healSkill);
        pet.SkillMastery[10] = new SkillMastery();

        // Define a damage skill
        var damageSkill = new Skill
        {
            SkillId = 20,
            Name = "Punch",
            SpCost = 5,
            TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage, Value = 10, Chance = 1.0f } }
        };
        _mockSkills.Setup(s => s.GetSkillById(20)).Returns(damageSkill);
        pet.SkillMastery[20] = new SkillMastery();

        // Act
        var action = _policy.GetAction(pet, participants, _mockRandom.Object);

        // Assert
        Assert.NotNull(action);
        Assert.Equal(10, action.SkillId); // Should pick Heal
        Assert.Equal(2, action.TargetId); // Should target low HP ally
    }

    [Fact]
    public void PetAI_ChoosesElementalAdvantage()
    {
        // Setup
        var pet = CreatePet(1, Team.Player, Element.Fire);
        pet.Sp = 100;

        var windEnemy = new ServerCharacter { Id = 3, Team = Team.Enemy, CharacterElement = Element.Wind, Con = 10, Hp = 100 };
        var earthEnemy = new ServerCharacter { Id = 4, Team = Team.Enemy, CharacterElement = Element.Earth, Con = 10, Hp = 100 };

        var participants = new List<ServerCombatant> { pet, windEnemy, earthEnemy };

        // Define a damage skill
        var damageSkill = new Skill
        {
            SkillId = 20,
            Name = "Fire Punch",
            SpCost = 5,
            TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage, Value = 10, Chance = 1.0f } }
        };
        _mockSkills.Setup(s => s.GetSkillById(20)).Returns(damageSkill);
        pet.SkillMastery[20] = new SkillMastery();

        // Act
        var action = _policy.GetAction(pet, participants, _mockRandom.Object);

        // Assert
        Assert.NotNull(action);
        Assert.Equal(20, action.SkillId);
        Assert.Equal(3, action.TargetId); // Should target windEnemy (Fire > Wind)
    }

    [Fact]
    public void PetAI_IsDeterministic()
    {
        // Setup
        var pet = CreatePet(1, Team.Player, Element.Earth);
        pet.Sp = 100;

        var enemy1 = new ServerCharacter { Id = 3, Team = Team.Enemy, Con = 10, Hp = 100 };
        var enemy2 = new ServerCharacter { Id = 4, Team = Team.Enemy, Con = 10, Hp = 100 };

        var participants = new List<ServerCombatant> { pet, enemy1, enemy2 };

        var damageSkill = new Skill
        {
            SkillId = 20,
            Name = "Punch",
            SpCost = 5,
            TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage, Value = 10, Chance = 1.0f } }
        };
        _mockSkills.Setup(s => s.GetSkillById(20)).Returns(damageSkill);
        pet.SkillMastery[20] = new SkillMastery();

        // Act
        var action1 = _policy.GetAction(pet, participants, _mockRandom.Object);
        var action2 = _policy.GetAction(pet, participants, _mockRandom.Object);

        // Assert
        Assert.NotNull(action1);
        Assert.NotNull(action2);
        Assert.Equal(action1.SkillId, action2.SkillId);
        Assert.Equal(action1.TargetId, action2.TargetId);
    }

    private ServerPet CreatePet(int id, Team team, Element element)
    {
        var pet = new ServerPet
        {
            Id = id,
            Team = team,
            CharacterElement = element,
            Con = 10,
            Agi = 10,
            Str = 5
        };
        // Manual HP/SP initialization if not using Hydrate
        pet.Hp = 100;
        pet.Sp = 50; 
        return pet;
    }
}
