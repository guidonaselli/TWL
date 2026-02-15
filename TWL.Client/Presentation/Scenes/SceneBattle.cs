using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Domain.Battle;
using TWL.Shared.Domain.Events;
using TWL.Shared.Domain.Skills;
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
    private readonly IAssetLoader _assets;

    // Skills
    private List<(string Name, int Id)> _availableSkills = new();
    private OfflineCombatManager _combat = null!;

    private SpriteFont _font = null!;

    // Input Debounce
    private KeyboardState _lastKs;
    private int _menuIndex; // 0: Attack, 1: Skill, 2: Defend
    private BattleStarted _payload = null!;
    private List<Combatant> _potentialTargets = new();

    // Result State
    private BattleFinished _result = null!;
    private string _cachedResultMsg = "";
    private CombatActionType _selectedActionType;
    private int _selectedSkillId;
    private int _skillIndex;
    private string _status = "Battle start!";
    private int _targetIndex;

    // UI State
    private BattleUiState _uiState = BattleUiState.Idle;
    private Texture2D _whiteTexture = null!;

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

        // Initialize skills from the first ally (Single Player assumption)
        // In a party system, we'd need to update this when the active character changes.
        // For now, since it's 1 player vs enemies, this is fine.
        var player = _payload.Allies.FirstOrDefault();
        if (player != null)
        {
            // For vertical slice, if no skills are known, give defaults.
            if (player.KnownSkills.Count == 0)
            {
                player.KnownSkills.Add(1); // Power Strike
                player.KnownSkills.Add(23); // Flame Strike (Data)
                player.KnownSkills.Add(20); // Fireball (Data)
                player.KnownSkills.Add(3); // Heal
                player.KnownSkills.Add(4); // Focus
            }

            _availableSkills = player.KnownSkills.Select(id => (GetSkillName(id), id)).ToList();
        }
    }

    private string GetSkillName(int id)
    {
        var skill = SkillRegistry.Instance.GetSkillById(id);
        if (skill != null)
        {
            return skill.Name;
        }

        switch (id)
        {
            case 1: return "Power Strike";
            case 2: return "Fireball";
            case 3: return "Heal";
            case 4: return "Focus";
            default: return "Unknown";
        }
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

        _cachedResultMsg = e.Victory ? "YOU WON! Press Enter" : "YOU LOST... Press Enter";
        if (e.Victory)
        {
            _cachedResultMsg += $"\nEXP Gained: {e.ExpGained}";
            if (e.Loot.Count > 0)
            {
                _cachedResultMsg += "\nLoot: " + string.Join(", ", e.Loot.Select(i => i.Name));
            }
        }
    }

    public override void Update(GameTime gt,
        MouseState ms,
        KeyboardState ks)
    {
        var dt = (float)gt.ElapsedGameTime.TotalSeconds;

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
            {
                _status = _combat.LastMessage;
            }

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
                {
                    _uiState = BattleUiState.Idle;
                }
            }
        }

        _lastKs = ks;
    }

    private void HandleInput(KeyboardState ks)
    {
        if (JustPressed(ks, Keys.Up))
        {
            if (_uiState == BattleUiState.Menu)
            {
                _menuIndex = (_menuIndex - 1 + 3) % 3;
            }
            else if (_uiState == BattleUiState.SkillSelection)
            {
                _skillIndex = (_skillIndex - 1 + _availableSkills.Count) % _availableSkills.Count;
            }
            else if (_uiState == BattleUiState.TargetSelection)
            {
                _targetIndex = (_targetIndex - 1 + _potentialTargets.Count) % _potentialTargets.Count;
            }
        }

        if (JustPressed(ks, Keys.Down))
        {
            if (_uiState == BattleUiState.Menu)
            {
                _menuIndex = (_menuIndex + 1) % 3;
            }
            else if (_uiState == BattleUiState.SkillSelection)
            {
                _skillIndex = (_skillIndex + 1) % _availableSkills.Count;
            }
            else if (_uiState == BattleUiState.TargetSelection)
            {
                _targetIndex = (_targetIndex + 1) % _potentialTargets.Count;
            }
        }

        if (JustPressed(ks, Keys.Enter) || JustPressed(ks, Keys.Space))
        {
            if (_uiState == BattleUiState.Menu)
            {
                SelectMenuOption();
            }
            else if (_uiState == BattleUiState.SkillSelection)
            {
                if (_availableSkills.Count > 0)
                {
                    _selectedSkillId = _availableSkills[_skillIndex].Id;

                    List<Combatant> targets;
                    if (_selectedSkillId == 3 || _selectedSkillId == 4) // Heal or Focus
                    {
                        targets = _combat.Battle.Allies.Where(a => a.Character.IsAlive()).ToList();
                    }
                    else
                    {
                        targets = _combat.Battle.Enemies.Where(e => e.Character.IsAlive()).ToList();
                    }

                    StartTargetSelection(targets);
                }
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
                {
                    _uiState = BattleUiState.SkillSelection;
                }
                else
                {
                    _uiState = BattleUiState.Menu;
                }
            }
            else if (_uiState == BattleUiState.SkillSelection)
            {
                _uiState = BattleUiState.Menu;
            }
        }

        // Debug/Cheat to exit
        if (ks.IsKeyDown(Keys.E))
        {
            _combat.ForceEndBattle();
        }
    }

    private void SelectMenuOption()
    {
        var actor = _combat.Battle.CurrentTurnCombatant;
        if (actor == null)
        {
            return;
        }

        switch (_menuIndex)
        {
            case 0: // Attack
                _selectedActionType = CombatActionType.Attack;
                StartTargetSelection(_combat.Battle.Enemies.Where(e => e.Character.IsAlive()).ToList());
                break;
            case 1: // Skill
                _selectedActionType = CombatActionType.Skill;
                _skillIndex = 0;

                // Refresh available skills based on current actor
                // In party play, we need to do this. For single player, it's already set in ReceivePayload but doing it again is safer.
                if (actor.Character.KnownSkills.Count == 0)
                {
                    // Fallback
                    actor.Character.KnownSkills.Add(1);
                    actor.Character.KnownSkills.Add(23);
                    actor.Character.KnownSkills.Add(20);
                    actor.Character.KnownSkills.Add(3);
                }

                _availableSkills = actor.Character.KnownSkills.Select(id => (GetSkillName(id), id)).ToList();

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
        if (targets.Count == 0)
        {
            return;
        }

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

    private bool JustPressed(KeyboardState ks, Keys key) => ks.IsKeyDown(key) && _lastKs.IsKeyUp(key);

    public override void Draw(SpriteBatch sb)
    {
        GraphicsDevice.Clear(Color.Black);

        sb.Begin();
        sb.DrawString(_font, _status, new Vector2(50, 20), Color.White);

        if (_uiState == BattleUiState.Result)
        {
            sb.DrawString(_font, _cachedResultMsg, new Vector2(200, 200), Color.Yellow);
            sb.End();
            return;
        }

        var startY = 60;

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
            // Draw menu or skill menu depending on what we are selecting targets for
            if (_selectedActionType == CombatActionType.Skill)
            {
                DrawSkillMenu(sb, new Vector2(150, 300));
            }
            else
            {
                DrawMenu(sb, new Vector2(50, 300));
            }
        }

        sb.End();
    }

    private void DrawGroup(SpriteBatch sb, List<Combatant> group, Vector2 pos, Color color)
    {
        var y = pos.Y;
        foreach (var c in group)
        {
            var hp = $"{c.Character.Health}/{c.Character.MaxHealth}";
            var sp = $"{c.Character.Sp}/{c.Character.MaxSp}";
            var name = c.Character.Name;
            if (!c.Character.IsAlive())
            {
                name += " (Dead)";
            }

            sb.DrawString(_font, $"{name}  HP:{hp}  SP:{sp}", new Vector2(pos.X, y), color);

            // Draw ATB Bar
            DrawBar(sb, new Rectangle((int)pos.X, (int)y + 20, 100, 5), c.Atb / 100.0, Color.Yellow);

            // Draw HP Bar
            var hpPct = (double)c.Character.Health / c.Character.MaxHealth;
            DrawBar(sb, new Rectangle((int)pos.X + 110, (int)y + 20, 50, 5), hpPct, Color.Red);

            // Draw SP Bar (Only for Allies usually, but simple to draw for all)
            var spPct = (double)c.Character.Sp / c.Character.MaxSp;
            DrawBar(sb, new Rectangle((int)pos.X + 110, (int)y + 28, 50, 3), spPct, Color.Blue);

            // Target Indicator
            if (_uiState == BattleUiState.TargetSelection && _potentialTargets.Count > _targetIndex &&
                _potentialTargets[_targetIndex] == c)
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
        var width = (int)(rect.Width * Math.Clamp(pct, 0, 1));
        sb.Draw(_whiteTexture, new Rectangle(rect.X, rect.Y, width, rect.Height), color);
    }

    private void DrawMenu(SpriteBatch sb, Vector2 pos)
    {
        // Background box
        sb.Draw(_whiteTexture, new Rectangle((int)pos.X - 10, (int)pos.Y - 10, 150, 100), Color.DarkSlateGray * 0.9f);

        string[] options = { "Attack", "Skill", "Defend" };
        for (var i = 0; i < options.Length; i++)
        {
            var c = _uiState == BattleUiState.Menu && _menuIndex == i ? Color.Yellow : Color.Gray;
            sb.DrawString(_font, options[i], new Vector2(pos.X, pos.Y + i * 25), c);
        }
    }

    private void DrawSkillMenu(SpriteBatch sb, Vector2 pos)
    {
        // Background box
        var h = _availableSkills.Count * 25 + 20;
        sb.Draw(_whiteTexture, new Rectangle((int)pos.X - 10, (int)pos.Y - 10, 200, h), Color.DarkSlateGray * 0.9f);

        for (var i = 0; i < _availableSkills.Count; i++)
        {
            var c = _uiState == BattleUiState.SkillSelection && _skillIndex == i ? Color.Yellow : Color.Gray;
            sb.DrawString(_font, _availableSkills[i].Name, new Vector2(pos.X, pos.Y + i * 25), c);
        }
    }
}
