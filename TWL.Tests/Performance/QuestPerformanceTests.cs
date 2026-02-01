using System.Diagnostics;
using System.Text.Json;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Quests;
using Xunit.Abstractions;

namespace TWL.Tests.Performance;

public class QuestPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public QuestPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private ServerQuestManager CreateQuestManager()
    {
        var manager = new ServerQuestManager();
        var quests = new List<QuestDefinition>();

        // Create 100 dummy quests
        for (var i = 1; i <= 100; i++)
        {
            quests.Add(new QuestDefinition
            {
                QuestId = i,
                Title = $"Quest {i}",
                Description = "Desc",
                Objectives = new List<ObjectiveDefinition>
                {
                    new("Talk", "NpcA", 1, "Talk to NPC A"),
                    new("Collect", "ItemB", 5, "Collect Item B"),
                    new("Interact", "ObjectC", 1, "Interact with C")
                },
                Rewards = new RewardDefinition(100, 50, new List<ItemReward>())
            });
        }

        var json = JsonSerializer.Serialize(quests);
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);

        manager.Load(tempFile);
        File.Delete(tempFile);

        return manager;
    }

    [Fact]
    public void Benchmark_HandleInteract_Allocations()
    {
        var questManager = CreateQuestManager();
        var component = new PlayerQuestComponent(questManager);

        // Start all quests
        for (var i = 1; i <= 100; i++)
        {
            component.StartQuest(i);
        }

        // Simulate logic from ClientSession.HandleInteractAsync
        // We will call TryProgress for Talk, Collect, Interact, Craft
        // for a target name "NpcA"

        const int iterations = 1000;
        var target = "NpcA";

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var initialMemory = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();

        for (var k = 0; k < iterations; k++)
        {
            var uniqueUpdates = new HashSet<int>();

            // Try "Talk", "Collect", "Interact"
            component.TryProgress(uniqueUpdates, target, "Talk", "Collect", "Interact");

            // Try "Craft"
            component.TryProgress(uniqueUpdates, target, "Craft");
        }

        sw.Stop();
        var finalMemory = GC.GetAllocatedBytesForCurrentThread();
        var allocated = finalMemory - initialMemory;

        _output.WriteLine($"Iterations: {iterations}");
        _output.WriteLine($"Time: {sw.ElapsedMilliseconds} ms");
        _output.WriteLine($"Allocated: {allocated / 1024.0:F2} KB");
        _output.WriteLine($"Allocated per op: {allocated / (double)iterations} bytes");

        // Assert that we are allocating a significant amount (baseline check)
        // With 100 active quests, TryProgress calls ToList on Dictionary (100 entries).
        // Plus List<int> results.
        // It should be > 0.
        Assert.True(allocated > 0);
    }
}