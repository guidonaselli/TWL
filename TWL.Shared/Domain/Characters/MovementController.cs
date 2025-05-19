using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TWL.Shared.Domain.Characters;

public class MovementController
{
    public static void UpdateMovement(PlayerCharacter player, GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();
        var moveDir = Vector2.Zero;

        if (keyboardState.IsKeyDown(Keys.W))
        {
            moveDir.Y -= 1;
            player.CurrentDirection = FacingDirection.Up;
        }

        if (keyboardState.IsKeyDown(Keys.S))
        {
            moveDir.Y += 1;
            player.CurrentDirection = FacingDirection.Down;
        }

        if (keyboardState.IsKeyDown(Keys.A))
        {
            moveDir.X -= 1;
            player.CurrentDirection = FacingDirection.Left;
        }

        if (keyboardState.IsKeyDown(Keys.D))
        {
            moveDir.X += 1;
            player.CurrentDirection = FacingDirection.Right;
        }

        if (moveDir.LengthSquared() > 0)
            Vector2.Normalize(moveDir);

        var nextPos = player.Position + moveDir * player.MovementSpeed;

        // Call a public collision check (make sure PlayerCharacter.IsColliding is public)
        if (!player.IsColliding(nextPos))
            player.Position = nextPos;

        // Clamp the position using publicly exposed map information
        float maxX = player.MapWidth * player.TileWidth - player.TileWidth;
        float maxY = player.MapHeight * player.TileHeight - player.TileHeight;

        player.Position = new Vector2(
            MathHelper.Clamp(player.Position.X, 0, maxX),
            MathHelper.Clamp(player.Position.Y, 0, maxY)
        );
    }
}