using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Managers;
using TWL.Client.Presentation.Services;
using TWL.Shared.Domain.Characters;

namespace TWL.Client.Presentation.UI;

public class UiMarketplace : UiWindow
{
    private readonly SpriteFont _font;
    private readonly MarketplaceManager _marketplace;

    private Texture2D? _bgTexture;
    private MouseState _oldMouse;

    private int _selectedListingId;
    private string _statusMsg = "";

    public UiMarketplace(Rectangle bounds, MarketplaceManager marketplace, SpriteFont font)
        : base(bounds)
    {
        _marketplace = marketplace;
        _font = font;
    }

    public override void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
    {
        if (!Visible)
        {
            return;
        }

        // Check for clicks
        if (mouseState.LeftButton == ButtonState.Pressed &&
            _oldMouse.LeftButton == ButtonState.Released)
        {
            // Ejemplo: asume un click en la lista de listings
            var clickPos = new Point(mouseState.X, mouseState.Y);
            if (Bounds.Contains(clickPos))
            {
                // Cálculo de qué listing clickeaste
                var relativeY = clickPos.Y - Bounds.Y;
                var index = relativeY / 20;
                // (básico, cada listing en 20 px)

                var listings = _marketplace.GetAllListings().ToList();
                if (index < listings.Count)
                {
                    _selectedListingId = listings[index].ListingId;
                }
            }
        }

        _oldMouse = mouseState;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!Visible)
        {
            return;
        }

        // Fondo
        spriteBatch.Draw(_bgTexture, Bounds, Color.White);

        var x = Bounds.X + 10;
        var y = Bounds.Y + 10;

        var listings = _marketplace.GetAllListings().ToList();
        for (var i = 0; i < listings.Count; i++)
        {
            var lst = listings[i];
            var text = Loc.TF("UI_MarketplaceListingFormat", lst.ListingId, lst.ItemId, lst.Quantity, lst.Price);
            spriteBatch.DrawString(_font, text, new Vector2(x, y),
                lst.ListingId == _selectedListingId ? Color.Yellow : Color.White);
            y += 20;
        }

        // Dibuja status
        spriteBatch.DrawString(_font, _statusMsg, new Vector2(x, Bounds.Bottom - 20), Color.Red);
    }

    public void AttemptPurchase(int buyerId, Inventory buyerInv, Inventory sellerInv, int qty)
    {
        if (_selectedListingId == 0)
        {
            return;
        }

        var status = "";
        var success = _marketplace.BuyListing(buyerId, _selectedListingId, qty, buyerInv, sellerInv, ref status);
        _statusMsg = status;
    }

    public void LoadBackground(Texture2D texture) => _bgTexture = texture;
}