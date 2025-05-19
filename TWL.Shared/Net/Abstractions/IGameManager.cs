namespace TWL.Shared.Net.Abstractions;

public interface IGameManager
{
    bool IsPaused { get; }
    void PauseGame();
    void ResumeGame();
}