using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Events;
using TWL.Shared.Domain.Models;
using TWL.Shared.Net;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.Scenes;

public enum BattleUiState
{
    Idle,
    Menu,
    SkillSelection,
    TargetSelection,
    Result
}

public sealed class SceneBattle : SceneBase, IPayloadReceiver
{
    private OfflineCombatManager _combat = null!;
    private readonly IAssetLoader _assets;

    private SpriteFont _font = null!;
    private Texture2D _whiteTexture = null!;
    private BattleStarted _payload = null!;
    private string _status = "Battle start!";

    // UI State
    private BattleUiState _uiState = BattleUiState.Idle;
    private int _menuIndex = 0; // 0: Attack, 1: Skill, 2: Defend
    private int _skillIndex = 0;
    private int _targetIndex = 0;
    private List<Combatant> _potentialTargets = new();
    private CombatActionType _selectedActionType;
    private int _selectedSkillId;

    // Hardcoded skills for vertical slice
    private readonly List<(string Name, int Id)> _availableSkills = new()
    {
        ("Power Strike", 1),
        ("Fireball", 2),
        ("Heal", 3)
    };

    // Result State
    private BattleFinished _result = null!;

    // Input Debounce
    private KeyboardState _lastKs;

    public SceneBattle(ContentManager content,
        GraphicsDevice gd,
        ISceneManager scenes,
        IAssetLoader assets)
        : base(content, gd, scenes, assets)
    {
    }

    public void ReceivePayload(object payload)
    {
        _payload = (BattleStarted)payload;
        _combat = new OfflineCombatManager(_payload.Allies, _payload.Enemies);
        _uiState = BattleUiState.Idle;
        _menuIndex = 0;
        _status = "Battle start!";
    }

    public override void LoadContent()
    {
        _font = Assets.Load<SpriteFont>("Fonts/UIFont");
        _whiteTexture = new Texture2D(GraphicsDevice, 1, 1);
        _whiteTexture.SetData(new[] { Color.White });
    }

    public override void Initialize()
    {
        base.Initialize();
        EventBus.Subscribe<BattleFinished>(OnBattleFinished);
    }

    private void OnBattleFinished(BattleFinished e)
    {
        _result = e;
        _uiState = BattleUiState.Result;
        _status = e.Victory ? "Victory!" : "Defeat...";
    }

    public override void Update(GameTime gt,
        MouseState ms,
        KeyboardState ks)
    {
        float dt = (float)gt.ElapsedGameTime.TotalSeconds;

        if (_uiState == BattleUiState.Result)
        {
            if (JustPressed(ks, Keys.Enter) || JustPressed(ks, Keys.Space) || JustPressed(ks, Keys.Escape))
            {
                Scenes.ChangeScene("Gameplay");
            }
        }
        else
        {
            _combat.Tick(dt);
            if (_uiState != BattleUiState.Result)
                 _status = _combat.LastMessage;

            // Input Handling
            if (_combat.State == LocalBattleState.AwaitingInput)
            {
                if (_uiState == BattleUiState.Idle)
                {
                    _uiState = BattleUiState.Menu;
                    _menuIndex = 0;
                }

                HandleInput(ks);
            }
            else
            {
                if (_uiState != BattleUiState.Result)
                     _uiState = BattleUiState.Idle;
            }
        }

        _lastKs = ks;
    }

    private void HandleInput(KeyboardState ks)
    {
        if (JustPressed(ks, Keys.Up))
        {
            if (_uiState == BattleUiState.Menu) _menuIndex = (_menuIndex - 1 + 3) % 3;
            else if (_uiState == BattleUiState.SkillSelection) _skillIndex = (_skillIndex - 1 + _availableSkills.Count) % _availableSkills.Count;
            else if (_uiState == BattleUiState.TargetSelection) _targetIndex = (_targetIndex - 1 + _potentialTargets.Count) % _potentialTargets.Count;
        }
        if (JustPressed(ks, Keys.Down))
        {
            if (_uiState == BattleUiState.Menu) _menuIndex = (_menuIndex + 1) % 3;
            else if (_uiState == BattleUiState.SkillSelection) _skillIndex = (_skillIndex + 1) % _availableSkills.Count;
            else if (_uiState == BattleUiState.TargetSelection) _targetIndex = (_targetIndex + 1) % _potentialTargets.Count;
        }

        if (JustPressed(ks, Keys.Enter) || JustPressed(ks, Keys.Space))
        {
            if (_uiState == BattleUiState.Menu)
            {
                SelectMenuOption();
            }
            else if (_uiState == BattleUiState.SkillSelection)
            {
                _selectedSkillId = _availableSkills[_skillIndex].Id;
                // Determine targets based on skill type (Heal targets allies, others enemies)
                // For simplicity, Heal targets anyone or allies. Let's say anyone for now, or just allies.
                // Looking at BattleInstance.UseSkill, Heal targets "target".

                List<Combatant> targets;
                if (_selectedSkillId == 3) // Heal
                    targets = _combat.Battle.Allies.Where(a => a.Character.IsAlive()).ToList();
                else
                    targets = _combat.Battle.Enemies.Where(e => e.Character.IsAlive()).ToList();

                StartTargetSelection(targets);
            }
            else if (_uiState == BattleUiState.TargetSelection)
            {
                ExecuteAction();
            }
        }

        if (JustPressed(ks, Keys.Escape))
        {
            if (_uiState == BattleUiState.TargetSelection)
            {
                if (_selectedActionType == CombatActionType.Skill)
                    _uiState = BattleUiState.SkillSelection;
                else
                    _uiState = BattleUiState.Menu;
            }
            else if (_uiState == BattleUiState.SkillSelection)
            {
                _uiState = BattleUiState.Menu;
            }
        }
    }

    private void SelectMenuOption()
    {
        var actor = _combat.Battle.CurrentTurnCombatant;
        if (actor == null) return;

        switch (_menuIndex)
        {
            case 0: // Attack
                _selectedActionType = CombatActionType.Attack;
                StartTargetSelection(_combat.Battle.Enemies.Where(e => e.Character.IsAlive()).ToList());
                break;
            case 1: // Skill
                _selectedActionType = CombatActionType.Skill;
                _skillIndex = 0;
                _uiState = BattleUiState.SkillSelection;
                break;
            case 2: // Defend
                _combat.PlayerAction(CombatAction.Defend(actor.BattleId));
                _uiState = BattleUiState.Idle;
                break;
        }
    }

    private void StartTargetSelection(List<Combatant> targets)
    {
        if (targets.Count == 0) return;
        _potentialTargets = targets;
        _targetIndex = 0;
        _uiState = BattleUiState.TargetSelection;
    }

    private void ExecuteAction()
    {
        var actor = _combat.Battle.CurrentTurnCombatant;
        var target = _potentialTargets[_targetIndex];

        if (_selectedActionType == CombatActionType.Attack)
        {
            _combat.PlayerAction(CombatAction.Attack(actor.BattleId, target.BattleId));
        }
        else if (_selectedActionType == CombatActionType.Skill)
        {
            _combat.PlayerAction(CombatAction.UseSkill(actor.BattleId, target.BattleId, _selectedSkillId));
        }

        _uiState = BattleUiState.Idle;
    }

    private bool JustPressed(KeyboardState ks, Keys key)
    {
        return ks.IsKeyDown(key) && _lastKs.IsKeyUp(key);
    }

    public override void Draw(SpriteBatch sb)
    {
        GraphicsDevice.Clear(Color.Black);

        sb.DrawString(_font, _status, new Vector2(50, 20), Color.White);

        if (_uiState == BattleUiState.Result)
        {
             string msg = _result.Victory ? "YOU WON! Press Enter" : "YOU LOST... Press Enter";
             if (_result.Victory)
             {
                 msg += $"\nEXP Gained: {_result.ExpGained}";
                 if (_result.Loot.Count > 0)
                 {
                     msg += "\nLoot: " + string.Join(", ", _result.Loot.Select(i => i.Name));
                 }
             }
             sb.DrawString(_font, msg, new Vector2(200, 200), Color.Yellow);
             return;
        }

        int startY = 60;

        // Allies
        DrawGroup(sb, _combat.Battle.Allies, new Vector2(50, startY), Color.LightGreen);

        // Enemies
        DrawGroup(sb, _combat.Battle.Enemies, new Vector2(400, startY), Color.IndianRed);

        // Menu
        if (_uiState == BattleUiState.Menu)
        {
            DrawMenu(sb, new Vector2(50, 300));
        }
        else if (_uiState == BattleUiState.SkillSelection)
        {
            DrawSkillMenu(sb, new Vector2(150, 300));
        }
        else if (_uiState == BattleUiState.TargetSelection)
        {
            // Just draw indicator (handled in DrawGroup)
            // But maybe re-draw menu to keep it visible?
             if (_selectedActionType == CombatActionType.Skill)
                 DrawSkillMenu(sb, new Vector2(150, 300));
             else
                 DrawMenu(sb, new Vector2(50, 300));
        }
    }

    private void DrawGroup(SpriteBatch sb, List<Combatant> group, Vector2 pos, Color color)
    {
        float y = pos.Y;
        foreach (var c in group)
        {
            string hp = $"{c.Character.Health}/{c.Character.MaxHealth}";
            string sp = $"{c.Character.Sp}/{c.Character.MaxSp}";
            string name = c.Character.Name;
            if (!c.Character.IsAlive()) name += " (Dead)";

            sb.DrawString(_font, $"{name}  HP:{hp}  SP:{sp}", new Vector2(pos.X, y), color);

            // Draw ATB Bar
            DrawBar(sb, new Rectangle((int)pos.X, (int)y + 20, 100, 5), c.Atb / 100.0, Color.Yellow);

            // Draw HP Bar
            double hpPct = (double)c.Character.Health / c.Character.MaxHealth;
            DrawBar(sb, new Rectangle((int)pos.X + 110, (int)y + 20, 50, 5), hpPct, Color.Red);

            // Target Indicator
            if (_uiState == BattleUiState.TargetSelection && _potentialTargets.Count > _targetIndex && _potentialTargets[_targetIndex] == c)
            {
                sb.DrawString(_font, "<--", new Vector2(pos.X + 200, y), Color.White);
            }

            y += 40;
        }
    }

    private void DrawBar(SpriteBatch sb, Rectangle rect, double pct, Color color)
    {
        // Background
        sb.Draw(_whiteTexture, rect, Color.Gray);
        // Foreground
        int width = (int)(rect.Width * Math.Clamp(pct, 0, 1));
        sb.Draw(_whiteTexture, new Rectangle(rect.X, rect.Y, width, rect.Height), color);
    }

    private void DrawMenu(SpriteBatch sb, Vector2 pos)
    {
        string[] options = { "Attack", "Skill", "Defend" };
        for (int i = 0; i < options.Length; i++)
        {
            Color c = (_uiState == BattleUiState.Menu && _menuIndex == i) ? Color.Yellow : Color.Gray;
            sb.DrawString(_font, options[i], new Vector2(pos.X, pos.Y + i * 25), c);
        }
    }

    private void DrawSkillMenu(SpriteBatch sb, Vector2 pos)
    {
        for (int i = 0; i < _availableSkills.Count; i++)
        {
            Color c = (_uiState == BattleUiState.SkillSelection && _skillIndex == i) ? Color.Yellow : Color.Gray;
            sb.DrawString(_font, _availableSkills[i].Name, new Vector2(pos.X, pos.Y + i * 25), c);
        }
    }
}
