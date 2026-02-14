using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.Characters;
using Xunit;

namespace TWL.Tests.Server;

public class MonsterManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MonsterManager _manager;

    public MonsterManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TWL_MonsterManagerTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        _manager = new MonsterManager();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private string CreateMonsterFile(string fileName, List<MonsterDefinition> monsters)
    {
        var path = Path.Combine(_tempDir, fileName);
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var json = JsonSerializer.Serialize(monsters, options);
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Load_ValidMonsters_Success()
    {
        var monsters = new List<MonsterDefinition>
        {
            new()
            {
                MonsterId = 1,
                Name = "Valid Monster",
                Element = Element.Fire,
                Tags = new List<string>()
            },
            new()
            {
                MonsterId = 2,
                Name = "Quest Dummy",
                Element = Element.None,
                Tags = new List<string> { "QuestOnly" }
            }
        };

        var path = CreateMonsterFile("valid_monsters.json", monsters);
        _manager.Load(path);

        var m1 = _manager.GetDefinition(1);
        Assert.NotNull(m1);
        Assert.Equal("Valid Monster", m1.Name);

        var m2 = _manager.GetDefinition(2);
        Assert.NotNull(m2);
        Assert.Equal("Quest Dummy", m2.Name);
    }

    [Fact]
    public void Load_ElementNone_MissingTag_ThrowsInvalidDataException()
    {
        var monsters = new List<MonsterDefinition>
        {
            new()
            {
                MonsterId = 3,
                Name = "Invalid Monster",
                Element = Element.None,
                Tags = new List<string>() // Missing QuestOnly
            }
        };

        var path = CreateMonsterFile("invalid_monsters.json", monsters);

        Assert.Throws<InvalidDataException>(() => _manager.Load(path));
    }

    [Fact]
    public void Load_DuplicateIds_LogsWarningAndOverwrites()
    {
        // Since MonsterManager logs to Console, we can't easily assert the log,
        // but we can check if the definition is overwritten or kept.
        // Assuming implementation overwrites (LWW - Last Write Wins) or ignores.
        // Looking at code: `_definitions[def.MonsterId] = def;` -> Overwrites.

        var monsters = new List<MonsterDefinition>
        {
            new() { MonsterId = 4, Name = "First", Element = Element.Water },
            new() { MonsterId = 4, Name = "Second", Element = Element.Water }
        };

        var path = CreateMonsterFile("duplicate_monsters.json", monsters);
        _manager.Load(path);

        var m = _manager.GetDefinition(4);
        Assert.NotNull(m);
        Assert.Equal("Second", m.Name);
    }
}
