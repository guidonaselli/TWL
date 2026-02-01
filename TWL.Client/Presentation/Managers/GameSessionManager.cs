namespace TWL.Client.Presentation.Managers;

public class GameSessionManager
{
    public PlayerCharacterData CurrentPlayer { get; private set; }
    public bool IsLoggedIn { get; private set; }

    public void StartSession(PlayerCharacterData playerData)
    {
        CurrentPlayer = playerData;
        IsLoggedIn = true;
    }

    public void EndSession()
    {
        CurrentPlayer = null;
        IsLoggedIn = false;
    }
}