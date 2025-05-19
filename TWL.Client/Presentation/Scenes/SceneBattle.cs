using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Domain.Events;
using TWL.Shared.Domain.Models;
using TWL.Shared.Net;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.Scenes;

/// <summary>Escena de combate en modo offline.</summary>
public sealed class SceneBattle : SceneBase, IPayloadReceiver
{
    private OfflineCombatManager _combat = null!;
    private readonly IAssetLoader _assets;

    private SpriteFont _font = null!;
    private BattleStarted _payload = null!;
    private string _status = "Battle start!";

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
        _combat = new OfflineCombatManager(
            _payload.Allies, _payload.Enemies);
    }

    public override void LoadContent() =>
        _font = Assets.Load<SpriteFont>("Fonts/BattleFont");

    public override void Initialize()
    {
        base.Initialize();
        EventBus.Subscribe<BattleFinished>(_ => Scenes.ChangeScene("Gameplay"));
    }

    public override void Update(GameTime gt,
        MouseState ms,
        KeyboardState ks)
    {
        if (ks.IsKeyDown(Keys.Space))
        {
            var enemy = _payload.Enemies.FirstOrDefault(e => e.Health > 0);
            if (enemy != null)
                _combat.PlayerAttack(enemy);
        }

        if (ks.IsKeyDown(Keys.E))
        {
            EventBus.Publish(new BattleFinished(false, 0, new List<Item>()));
            return;
        }

        _combat.Tick();
        _status = _combat.LastMessage;
    }

    public override void Draw(SpriteBatch sb)
    {
        // (opcional) si quieres un fondo negro solo en batalla…
        GraphicsDevice.Clear(Color.Black);

        // aquí ya NO hay Begin/End
        sb.DrawString(_font, _status, new Vector2(50, 50), Color.White);
        sb.DrawString(_font, "SPACE = attack   E = flee",
            new Vector2(50, 70), Color.Yellow);

        // …y el resto sigue igual
        int y = 100;
        sb.DrawString(_font, "--- Allies ---", new Vector2(50, y), Color.Green);
        y += 20;
        foreach (var a in _payload.Allies)
        {
            sb.DrawString(_font, $"{a.Name} {a.Health}/{a.MaxHealth}",
                new Vector2(50, y), Color.Green);
            y += 20;
        }

        y += 40;
        sb.DrawString(_font, "--- Enemies ---", new Vector2(50, y), Color.Red);
        y += 20;
        foreach (var e in _payload.Enemies)
        {
            sb.DrawString(_font, $"{e.Name} {e.Health}/{e.MaxHealth}",
                new Vector2(50, y), Color.Red);
            y += 20;
        }
    }
}