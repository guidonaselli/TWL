using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TWL.Client.Presentation.Managers;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.Core
{
    /// <summary>
    /// Controla el estado global del juego (pausa, reanudación)
    /// y delega el ciclo de actualización a la escena activa.
    /// </summary>
    public class GameManager : IGameManager
    {
        private readonly SceneManager _sceneManager;

        /// <summary>Indica si el juego está en pausa (detiene lógica de escenas).</summary>
        public bool IsPaused { get; private set; }

        public GameManager(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        /// <summary>Pone el juego en pausa (las escenas dejarán de actualizarse).</summary>
        public void PauseGame()  => IsPaused = true;

        /// <summary>Reanuda el juego (las escenas vuelven a actualizarse).</summary>
        public void ResumeGame() => IsPaused = false;

        /// <summary>
        /// Debes llamar a este método desde tu Game1.Update().
        /// Leerá entrada y, si no estamos en pausa, actualizará la escena activa.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (IsPaused)
                return;

            // Capturamos estado de entrada
            MouseState    mouse    = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();

            // Delegamos a la escena activa
            _sceneManager.Update(gameTime, mouse, keyboard);
        }
    }
}