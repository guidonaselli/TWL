using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TWL.Client.UI;

namespace TWL.Client.Presentation.UI;

public class UiManager
{
    private readonly List<UiWindow> _windows;

    public UiManager()
    {
        _windows = new List<UiWindow>();
    }

    public void AddWindow(UiWindow window)
    {
        _windows.Add(window);
    }

    public void Update(GameTime gameTime, MouseState mouse, KeyboardState keyboard)
    {
        // pasa input solo a la ventana visible que contiene el ratón
        foreach (var wnd in _windows)
            if (wnd.Active && wnd.Visible && wnd.Contains(mouse.Position))
            {
                wnd.Update(gameTime, mouse, keyboard);
                return; // evita propagar a las demás
            }

        // si ninguna ventana consumió el click puedes, opcionalmente,
        // enviar el input al juego/mundo aquí.
    }


    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var wnd in _windows)
            if (wnd.Visible)
                wnd.Draw(spriteBatch);
    }
}