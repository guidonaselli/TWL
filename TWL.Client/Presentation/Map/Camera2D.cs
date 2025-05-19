using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TWL.Client.Presentation.Map;

public class Camera2D
{
    private Viewport _viewport;

    public Camera2D()
    {
        Position = Vector2.Zero;
        Zoom = 1f;
    }

    public Camera2D(Viewport viewport)
    {
        _viewport = viewport;
        Origin = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
    }

    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Rotation { get; set; } = 0f;
    public float Zoom { get; set; } = 1f;
    public Vector2 Origin { get; set; }

    public Matrix GetTransformation(GraphicsDevice gd)
    {
        return
            Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
            Matrix.CreateScale(Zoom) *
            Matrix.CreateTranslation(
                gd.Viewport.Width * 0.5f,
                gd.Viewport.Height * 0.5f,
                0
            );
    }

    public Vector2 ScreenToWorld(Vector2 screenPos, GraphicsDevice gd)
    {
        var inverse = Matrix.Invert(GetTransformation(gd));
        var worldPos = Vector3.Transform(new Vector3(screenPos, 0), inverse);
        return new Vector2(worldPos.X, worldPos.Y);
    }

    public Matrix GetViewMatrix()
    {
        // Create the view matrix for the camera
        return Matrix.CreateTranslation(new Vector3(-Position, 0.0f)) *
               Matrix.CreateRotationZ(Rotation) *
               Matrix.CreateScale(new Vector3(Zoom, Zoom, 1.0f)) *
               Matrix.CreateTranslation(new Vector3(Origin, 0.0f));
    }
}