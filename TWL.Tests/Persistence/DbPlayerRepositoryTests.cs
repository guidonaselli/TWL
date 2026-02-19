using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Database.Entities;
using TWL.Server.Persistence.Repositories;
using TWL.Server.Persistence.Repositories.Queries;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;
using TWL.Shared.Domain.Requests;
using TWL.Server.Persistence;
using Xunit;

namespace TWL.Tests.Persistence;

public class DbPlayerRepositoryTests
{
    private readonly Mock<IDbContextFactory<GameDbContext>> _mockContextFactory;
    private readonly Mock<IDbConnectionFactory> _mockConnectionFactory;
    private readonly Mock<ILogger<DbPlayerRepository>> _mockLogger;
    private readonly DbPlayerRepository _repository;
    private readonly GameDbContext _dbContext;

    public DbPlayerRepositoryTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        // We use a factory method to return a new context instance each time,
        // but connected to the same in-memory database.
        _mockContextFactory = new Mock<IDbContextFactory<GameDbContext>>();
        _mockContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new GameDbContext(options)));

        // Create a separate context for assertions to avoid ObjectDisposedException
        _dbContext = new GameDbContext(options);

        _mockConnectionFactory = new Mock<IDbConnectionFactory>();
        _mockLogger = new Mock<ILogger<DbPlayerRepository>>();

        _repository = new DbPlayerRepository(
            _mockContextFactory.Object,
            _mockConnectionFactory.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SaveAsync_NewPlayer_ShouldCreateEntity()
    {
        // Arrange
        var userId = 1;
        var saveData = new PlayerSaveData
        {
            Character = new ServerCharacterData { Id = 10, Name = "NewHero", Hp = 100 },
            Quests = new QuestData(),
            LastSaved = DateTime.UtcNow
        };

        // Act
        await _repository.SaveAsync(userId, saveData);

        // Assert
        var entity = await _dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);
        Assert.NotNull(entity);
        Assert.Equal("NewHero", entity.Name);
        Assert.Equal(100, entity.Hp);
        Assert.NotEqual(DateTime.MinValue, entity.LastSaved);
    }

    [Fact]
    public async Task SaveAsync_ExistingPlayer_ShouldUpdateEntity()
    {
        // Arrange
        var userId = 2;
        var existingEntity = new PlayerEntity { UserId = userId, Name = "OldName", Hp = 50 };
        _dbContext.Players.Add(existingEntity);
        await _dbContext.SaveChangesAsync();

        var saveData = new PlayerSaveData
        {
            Character = new ServerCharacterData { Id = 20, Name = "UpdatedName", Hp = 200 },
            Quests = new QuestData(),
            LastSaved = DateTime.UtcNow
        };

        // Act
        await _repository.SaveAsync(userId, saveData);

        // Assert
        // We must refresh the context or use a new one to see changes made by the repository
        // because the repository used a different context instance (but same DB).
        // Since _dbContext already tracked 'existingEntity', we need to reload it.
        await _dbContext.Entry(existingEntity).ReloadAsync();

        Assert.Equal("UpdatedName", existingEntity.Name);
        Assert.Equal(200, existingEntity.Hp);
    }

    [Fact]
    public async Task SaveAsync_ComplexProperties_ShouldSerializeCorrectly()
    {
        // Arrange
        var userId = 3;
        var inventory = new List<Item> { new Item { ItemId = 101, Quantity = 5 } };
        var pets = new List<ServerPetData> { new ServerPetData { DefinitionId = 500, Exp = 1000 } };

        var saveData = new PlayerSaveData
        {
            Character = new ServerCharacterData
            {
                Id = 30,
                Name = "ComplexHero",
                Inventory = inventory,
                Pets = pets
            },
            Quests = new QuestData(),
            LastSaved = DateTime.UtcNow
        };

        // Act
        await _repository.SaveAsync(userId, saveData);

        // Assert
        var entity = await _dbContext.Players.FirstOrDefaultAsync(p => p.UserId == userId);
        Assert.NotNull(entity);

        // Check JSON columns directly
        // Item uses [JsonPropertyName("id")] and [JsonPropertyName("q")]
        Assert.Contains("\"id\":101", entity.InventoryJson);
        Assert.Contains("\"q\":5", entity.InventoryJson);

        // ServerPetData properties use default naming
        Assert.Contains("\"DefinitionId\":500", entity.PetsJson);
    }

    // Testing LoadAsync is tricky due to Dapper extension methods on DbConnection.
    // For now, we will assume SaveAsync works and verify compilation.
    // Ideally, we would need to wrap Dapper calls in an interface (e.g. IDapperService)
    // to mock them easily.
    /*
    [Fact]
    public async Task LoadAsync_ShouldReturnData_WhenExists()
    {
        // Requires tedious DbConnection/DbCommand/DbDataReader mocking
    }
    */
}
