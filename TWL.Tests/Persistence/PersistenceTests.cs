using TWL.Server.Persistence;
using TWL.Server.Simulation.Networking;

namespace TWL.Tests.Persistence;

/// <summary>
/// In-memory IPlayerRepository for testing without DB or filesystem dependencies.
/// </summary>
internal class InMemoryPlayerRepository : IPlayerRepository
{
    private readonly Dictionary<int, PlayerSaveData> _store = new();

    public Task SaveAsync(int userId, PlayerSaveData data)
    {
        _store[userId] = data;
        return Task.CompletedTask;
    }

    public PlayerSaveData? Load(int userId) =>
        _store.TryGetValue(userId, out var data) ? data : null;

    public Task<PlayerSaveData?> LoadAsync(int userId) =>
        Task.FromResult(Load(userId));
}

public class PersistenceTests
{
    [Fact]
    public async Task Repo_SaveAndLoad_CharacterData()
    {
        var repo = new InMemoryPlayerRepository();
        var data = new PlayerSaveData
        {
            Character = new ServerCharacterData { Id = 1, Name = "TestUser", Hp = 50, Gold = 100 },
            Quests = new QuestData(),
            LastSaved = DateTime.UtcNow
        };

        await repo.SaveAsync(1, data);

        var loaded = repo.Load(1);
        Assert.NotNull(loaded);
        Assert.Equal("TestUser", loaded.Character.Name);
        Assert.Equal(50, loaded.Character.Hp);
        Assert.Equal(100, loaded.Character.Gold);
    }

    [Fact]
    public void ServerCharacter_IsDirty_OnModification()
    {
        var ch = new ServerCharacter { Id = 1, Name = "Test" };
        Assert.False(ch.IsDirty);

        ch.AddGold(10);
        Assert.True(ch.IsDirty);
        ch.IsDirty = false;

        ch.AddItem(1, 1);
        Assert.True(ch.IsDirty);
        ch.IsDirty = false;

        ch.AddExp(50);
        Assert.True(ch.IsDirty);
    }

    [Fact]
    public void ServerCharacter_GetAndLoadSaveData()
    {
        var ch = new ServerCharacter { Id = 10, Name = "Hero" };
        ch.AddGold(500);
        ch.AddItem(99, 5);

        var data = ch.GetSaveData();
        Assert.Equal(10, data.Id);
        Assert.Equal("Hero", data.Name);
        Assert.Equal(500, data.Gold);
        Assert.Single(data.Inventory);
        Assert.Equal(99, data.Inventory[0].ItemId);

        var ch2 = new ServerCharacter();
        ch2.LoadSaveData(data);

        Assert.Equal(10, ch2.Id);
        Assert.Equal("Hero", ch2.Name);
        Assert.Equal(500, ch2.Gold);
        Assert.True(ch2.HasItem(99, 5));
    }
}