using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Shared.Domain.Characters;

namespace TWL.Client.Presentation.UI;

public class UiInventoryWindow : UiWindow
{
    private const int SlotsPerRow = 5;
    private const int SlotSize = 48;
    private const int Padding = 10;
    private readonly Inventory _inventory;
    private readonly List<Rectangle> _slots = new();

    private Texture2D? _bg;
    private SpriteFont? _font;

    private int _selectedSlot = -1;
    private Texture2D? _slotTex;

    // ------------------------------------------------------------------

    public UiInventoryWindow(Rectangle bounds, Inventory inv) : base(bounds)
    {
        _inventory = inv;
        InitSlotRects();
    }

    // ---------- carga --------------------------------------------------

    public void LoadContent(ContentManager content)
    {
        _bg = SafeLoadTex(content, "UI/inventory_background");
        _slotTex = SafeLoadTex(content, "UI/inventory_slot");
        _font = content.Load<SpriteFont>("Fonts/DefaultFont");
    }

    private static Texture2D SafeLoadTex(ContentManager c, string asset)
    {
        try
        {
            return c.Load<Texture2D>(asset);
        }
        catch
        {
            var gd = ((IGraphicsDeviceService)c.ServiceProvider
                .GetService(typeof(IGraphicsDeviceService))).GraphicsDevice;
            var tex = new Texture2D(gd, 1, 1);
            tex.SetData(new[] { Color.White });
            return tex; // nunca devuelve null
        }
    }

    // ---------- slots --------------------------------------------------

    private void InitSlotRects()
    {
        _slots.Clear();

        var startX = Bounds.X + Padding;
        var startY = Bounds.Y + Padding;

        for (var i = 0; i < 20; i++)
        {
            var row = i / SlotsPerRow;
            var col = i % SlotsPerRow;
            _slots.Add(new Rectangle(
                startX + col * (SlotSize + Padding),
                startY + row * (SlotSize + Padding),
                SlotSize, SlotSize));
        }
    }

    // ---------- input --------------------------------------------------

    public override void Update(GameTime gt, MouseState ms, KeyboardState ks)
    {
        if (!Visible)
        {
            return;
        }

        if (ms.LeftButton == ButtonState.Pressed)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Contains(ms.Position))
                {
                    _selectedSlot = i;
                }
            }
        }
    }

    // ---------- render -------------------------------------------------

    public override void Draw(SpriteBatch sb)
    {
        if (!Visible || _bg is null || _slotTex is null || _font is null)
        {
            return;
        }

        sb.Draw(_bg, Bounds, Color.White);

        for (var i = 0; i < _slots.Count; i++)
        {
            sb.Draw(_slotTex, _slots[i], Color.White);

            if (i < _inventory.ItemSlots.Count)
            {
                var slot = _inventory.ItemSlots[i];

                // (dibujo del icono de item iría aquí)

                if (slot.Quantity > 1)
                {
                    var qty = slot.Quantity.ToString();
                    var size = _font.MeasureString(qty);
                    sb.DrawString(_font, qty,
                        new Vector2(_slots[i].Right - size.X - 4,
                            _slots[i].Bottom - size.Y - 2),
                        Color.SaddleBrown);
                }
            }

            if (i == _selectedSlot)
            {
                sb.Draw(_slotTex, _slots[i], new Color(255, 255, 0, 80));
            }
        }
    }
}