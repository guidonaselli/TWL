using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TWL.Shared.Net.Abstractions;

public interface IScene
{
    bool IsInitialized { get; }
    void Initialize();
    void LoadContent();
    void UnloadContent();
    void Update(GameTime time,
        MouseState mouse,
        KeyboardState keyboard);
    void Draw(SpriteBatch spriteBatch);
}