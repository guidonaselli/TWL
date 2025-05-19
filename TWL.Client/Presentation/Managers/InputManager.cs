using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Core;

namespace TWL.Client.Presentation.Managers;

public class InputManager : Singleton<InputManager>
{
    private KeyboardState _currentKeyboard, _previousKeyboard;
    private MouseState _currentMouse, _previousMouse;

    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;

        _currentKeyboard = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
    }

    public bool IsKeyPressed(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }

    public bool IsLeftMouseClicked()
    {
        return _currentMouse.LeftButton == ButtonState.Pressed &&
               _previousMouse.LeftButton == ButtonState.Released;
    }
}