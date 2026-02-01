using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Managers;
using TWL.Client.Presentation.UI;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.Scenes;

public sealed class SceneMarketplace : SceneBase
{
    private readonly MarketplaceManager _marketplace;
    private SpriteFont _font = null!;
    private UiMarketplace _uiMarketplace = null!;

    public SceneMarketplace(ContentManager content,
        GraphicsDevice gd,
        ISceneManager scenes,
        IAssetLoader assets,
        MarketplaceManager market)
        : base(content, gd, scenes, assets)
    {
        _marketplace = market;
    }

    public override void LoadContent()
    {
        _font = Assets.Load<SpriteFont>("Fonts/MenuFont");

        _uiMarketplace = new UiMarketplace(
            new Rectangle(50, 50, 400, 300),
            _marketplace,
            _font);

        _uiMarketplace.LoadBackground(
            Content.Load<Texture2D>("UI/PanelBg"));
    }

    public override void Update(GameTime t,
        MouseState m,
        KeyboardState k) =>
        _uiMarketplace.Update(t, m, k);

    public override void Draw(SpriteBatch sb)
    {
        sb.Begin();
        sb.DrawString(_font, "Marketplace",
            new Vector2(20, 20), Color.Black);
        _uiMarketplace.Draw(sb);
        sb.End();
    }
}
