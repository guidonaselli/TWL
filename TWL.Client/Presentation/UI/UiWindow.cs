using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TWL.Client.Presentation.UI;

public abstract class UiWindow
{
    // The bounds/position of the window
    protected Rectangle _bounds;

    protected UiWindow(Rectangle bounds)
    {
        _bounds = bounds;
    }

    // Public property to access bounds
    public Rectangle Bounds => _bounds;

    // Whether the window is visible
    public bool Visible { get; set; } = true;

    // Whether the window can be interacted with
    public bool Active { get; set; } = true;

    // Virtual methods for derived classes to override
    public virtual void Update(GameTime gameTime, MouseState mouse, KeyboardState keyboard)
    {
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
    }

    // Helper method to check if a point is inside this window
    public bool Contains(Point point)
    {
        return Visible && _bounds.Contains(point);
    }

    // Optional: Methods to move/resize the window
    public virtual void Move(Point newPosition)
    {
        _bounds.X = newPosition.X;
        _bounds.Y = newPosition.Y;
    }

    public virtual void Resize(int width, int height)
    {
        _bounds.Width = width;
        _bounds.Height = height;
    }
}