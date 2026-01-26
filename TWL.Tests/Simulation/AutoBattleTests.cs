using System.Collections.Generic;
using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests.Simulation;

public class AutoBattleTests
{
    private AutoBattleService _service;
    private FakeSkillCatalog _fakeSkills;
    private ServerCharacter _actor;
    private ServerCharacter _ally;
    private ServerCharacter _enemy;

    public AutoBattleTests()
    {
        _fakeSkills = new FakeSkillCatalog();
        _service = new AutoBattleService(_fakeSkills);

        _actor = new ServerCharacter { Id = 1, Name = "Actor", Int = 10, Sp = 100 };
        _ally = new ServerCharacter { Id = 2, Name = "Ally", Con = 10 }; // MaxHP 100
        _enemy = new ServerCharacter { Id = 3, Name = "Enemy", Con = 10 };
        _ally.Heal(100);
        _enemy.Heal(50);
    }

    [Fact]
    public void SelectAction_LowHpAlly_ShouldHeal()
    {
        _ally.ApplyDamage(80); // HP 20/100

        var healSkillId = 10;
        _actor.KnownSkills.Add(healSkillId);
        _fakeSkills.AddSkill(new Skill
        {
            SkillId = healSkillId,
            SpCost = 10,
            TargetType = SkillTargetType.SingleAlly,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Heal } }
        });

        var action = _service.SelectAction(_actor, new List<ServerCharacter> { _actor, _ally }, new List<ServerCharacter> { _enemy }, 12345);

        Assert.Equal(CombatActionType.Skill, action.Type);
        Assert.Equal(healSkillId, action.SkillId);
        Assert.Equal(_ally.Id, action.TargetId);
    }

    [Fact]
    public void SelectAction_Determinism_ShouldReturnSameActionForSameSeed()
    {
        // Setup multiple choices to test randomness/determinism
        var atkSkillId = 20;
        _actor.KnownSkills.Add(atkSkillId);
        _fakeSkills.AddSkill(new Skill
        {
            SkillId = atkSkillId,
            SpCost = 10,
            TargetType = SkillTargetType.SingleEnemy,
            Effects = new List<SkillEffect> { new SkillEffect { Tag = SkillEffectTag.Damage } }
        });

        var action1 = _service.SelectAction(_actor, new List<ServerCharacter> { _actor }, new List<ServerCharacter> { _enemy }, 999);
        var action2 = _service.SelectAction(_actor, new List<ServerCharacter> { _actor }, new List<ServerCharacter> { _enemy }, 999);

        Assert.Equal(action1.Type, action2.Type);
        Assert.Equal(action1.SkillId, action2.SkillId);
        Assert.Equal(action1.TargetId, action2.TargetId);
    }

    // Fake Implementation
    class FakeSkillCatalog : ISkillCatalog
    {
        private Dictionary<int, Skill> _skills = new();
        public void AddSkill(Skill skill) => _skills[skill.SkillId] = skill;
        public Skill? GetSkillById(int id) => _skills.ContainsKey(id) ? _skills[id] : null;
        public IEnumerable<int> GetAllSkillIds() => _skills.Keys;
    }
}
