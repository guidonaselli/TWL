using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Database;
using TWL.Server.Persistence.Database.Entities;
using TWL.Server.Persistence.Repositories;
using TWL.Server.Persistence.Repositories.Queries;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.Persistence;

public class DbPlayerRepositoryTests
{
    private readonly Mock<IDbContextFactory<GameDbContext>> _mockContextFactory;
    private readonly Mock<IDapperService> _mockDapperService;
    private readonly Mock<ILogger<DbPlayerRepository>> _mockLogger;
    private readonly DbContextOptions<GameDbContext> _dbOptions;

    public DbPlayerRepositoryTests()
    {
        _mockContextFactory = new Mock<IDbContextFactory<GameDbContext>>();
        _mockDapperService = new Mock<IDapperService>();
        _mockLogger = new Mock<ILogger<DbPlayerRepository>>();

        // Use a unique database name per test to avoid state pollution
        _dbOptions = new DbContextOptionsBuilder<GameDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Setup the factory to return a new context instance connected to the same InMemory DB
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new GameDbContext(_dbOptions));
    }

    [Fact]
    public async Task LoadAsync_ReturnsNull_WhenPlayerDoesNotExist()
    {
        // Arrange
        int userId = 1;
        _mockDapperService
            .Setup(d => d.QueryFirstOrDefaultAsync<PlayerDto>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                null, null, null))
            .ReturnsAsync((PlayerDto?)null);

        var repository = new DbPlayerRepository(
            _mockContextFactory.Object,
            _mockDapperService.Object,
            _mockLogger.Object);

        // Act
        var result = await repository.LoadAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadAsync_ReturnsData_WhenPlayerExists()
    {
        // Arrange
        int userId = 1;
        var playerDto = new PlayerDto
        {
            PlayerId = 10,
            UserId = userId,
            Name = "Hero",
            Hp = 100,
            // Use JSON structure matching Item serialization (JsonPropertyName "id" for ItemId)
            InventoryJson = "[{\"id\":1,\"q\":1}]"
        };

        _mockDapperService
            .Setup(d => d.QueryFirstOrDefaultAsync<PlayerDto>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                null, null, null))
            .ReturnsAsync(playerDto);

        var repository = new DbPlayerRepository(
            _mockContextFactory.Object,
            _mockDapperService.Object,
            _mockLogger.Object);

        // Act
        var result = await repository.LoadAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Character.Id);
        Assert.Equal("Hero", result.Character.Name);
        Assert.Single(result.Character.Inventory);
        Assert.Equal(1, result.Character.Inventory[0].ItemId);
        Assert.Equal(1, result.Character.Inventory[0].Quantity);
    }

    [Fact]
    public async Task SaveAsync_InsertsNewPlayer_WhenPlayerDoesNotExist()
    {
        // Arrange
        int userId = 2;
        var saveData = new PlayerSaveData
        {
            Character = new ServerCharacterData
            {
                Name = "NewHero",
                Hp = 50,
                Inventory = new List<Item> { new Item { ItemId = 2, Quantity = 5 } }
            },
            Quests = new QuestData(),
            LastSaved = DateTime.UtcNow
        };

        var repository = new DbPlayerRepository(
            _mockContextFactory.Object,
            _mockDapperService.Object,
            _mockLogger.Object);

        // Act
        await repository.SaveAsync(userId, saveData);

        // Assert
        // Check InMemory DB
        using var context = new GameDbContext(_dbOptions);
        var entity = await context.Players.FirstOrDefaultAsync(p => p.UserId == userId);

        Assert.NotNull(entity);
        Assert.Equal("NewHero", entity.Name);
        Assert.Equal(50, entity.Hp);
        // Verify JSON content. Item uses "id" and "q".
        Assert.Contains("\"id\":2", entity.InventoryJson);
        Assert.Contains("\"q\":5", entity.InventoryJson);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingPlayer_WhenPlayerExists()
    {
        // Arrange
        int userId = 3;

        // Seed existing player
        using (var context = new GameDbContext(_dbOptions))
        {
            context.Players.Add(new PlayerEntity
            {
                UserId = userId,
                Name = "OldName",
                Hp = 20,
                InventoryJson = "[]"
            });
            await context.SaveChangesAsync();
        }

        var saveData = new PlayerSaveData
        {
            Character = new ServerCharacterData
            {
                Name = "UpdatedName",
                Hp = 120,
                Inventory = new List<Item>()
            },
            Quests = new QuestData(),
            LastSaved = DateTime.UtcNow
        };

        var repository = new DbPlayerRepository(
            _mockContextFactory.Object,
            _mockDapperService.Object,
            _mockLogger.Object);

        // Act
        await repository.SaveAsync(userId, saveData);

        // Assert
        using (var context = new GameDbContext(_dbOptions))
        {
            var entity = await context.Players.FirstOrDefaultAsync(p => p.UserId == userId);
            Assert.NotNull(entity);
            Assert.Equal("UpdatedName", entity.Name);
            Assert.Equal(120, entity.Hp);
        }
    }

    [Fact]
    public async Task SaveAsync_VerifiesJsonSerialization()
    {
        // Arrange
        int userId = 4;
        var saveData = new PlayerSaveData
        {
            Character = new ServerCharacterData
            {
                Name = "JsonHero",
                Pets = new List<ServerPetData>
                {
                    new ServerPetData { InstanceId = "pet1", DefinitionId = 100 }
                },
                Skills = new List<SkillMasteryData>
                {
                    new SkillMasteryData { SkillId = 5, Rank = 2 }
                }
            },
            Quests = new QuestData(),
            LastSaved = DateTime.UtcNow
        };

        var repository = new DbPlayerRepository(
            _mockContextFactory.Object,
            _mockDapperService.Object,
            _mockLogger.Object);

        // Act
        await repository.SaveAsync(userId, saveData);

        // Assert
        using (var context = new GameDbContext(_dbOptions))
        {
            var entity = await context.Players.FirstOrDefaultAsync(p => p.UserId == userId);
            Assert.NotNull(entity);

            // Verify PetsJson
            Assert.Contains("pet1", entity.PetsJson);
            // DefinitionId is likely "DefinitionId"
            Assert.Contains("\"DefinitionId\":100", entity.PetsJson);

            // Verify SkillsJson
            Assert.Contains("\"SkillId\":5", entity.SkillsJson);
            Assert.Contains("\"Rank\":2", entity.SkillsJson);
        }
    }
}
