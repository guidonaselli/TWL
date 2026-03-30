using System.Diagnostics;
using Moq;
using TWL.Server.Persistence;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Guilds;
using TWL.Shared.Net.Network;
using Xunit;
using Xunit.Abstractions;

namespace TWL.Tests.Performance;

public class GuildRosterPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public GuildRosterPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Benchmark_SendFullRoster_Optimized()
    {
        var memberCount = 50;
        var ioDelayMs = 10;
        var requesterId = 1;
        var guildId = 101;

        var guildRepoMock = new Mock<IGuildRepository>();
        var guildManager = new GuildManager(guildRepoMock.Object);

        var guild = new TWL.Shared.Domain.Guilds.Guild
        {
            GuildId = guildId,
            Name = "PerfTestGuild",
            LeaderId = requesterId,
            MemberIds = Enumerable.Range(1, memberCount).ToList()
        };
        foreach (var id in guild.MemberIds)
        {
            guild.MemberRanks[id] = GuildRank.Recruit;
        }

        // Setup internal state of guildManager
        var guildsField = typeof(GuildManager).GetField("_guilds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playerGuildMapField = typeof(GuildManager).GetField("_playerGuildMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var guilds = (System.Collections.Concurrent.ConcurrentDictionary<int, TWL.Shared.Domain.Guilds.Guild>)guildsField.GetValue(guildManager);
        var playerGuildMap = (System.Collections.Concurrent.ConcurrentDictionary<int, int>)playerGuildMapField.GetValue(guildManager);

        guilds[guildId] = guild;
        foreach (var id in guild.MemberIds)
        {
            playerGuildMap[id] = guildId;
        }

        var playerServiceMock = new Mock<PlayerService>(null, null);

        // Requester session
        var requesterSessionMock = new Mock<ClientSession>();
        requesterSessionMock.Setup(s => s.Character).Returns(new ServerCharacter { Id = requesterId, Name = "Requester", Level = 10 });
        requesterSessionMock.Setup(s => s.SendAsync(It.IsAny<NetMessage>())).Returns(Task.CompletedTask);

        playerServiceMock.Setup(ps => ps.GetSession(requesterId)).Returns(requesterSessionMock.Object);

        // Other members are offline
        for (int i = 2; i <= memberCount; i++)
        {
            int id = i;
            playerServiceMock.Setup(ps => ps.GetSession(id)).Returns((ClientSession)null);
            playerServiceMock.Setup(ps => ps.LoadDataAsync(id)).ReturnsAsync(() =>
            {
                Thread.Sleep(ioDelayMs); // Simulate synchronous IO delay
                return new PlayerSaveData
                {
                    Character = new ServerCharacterData { Id = id, Name = $"Member_{id}", Level = 1, LastLoginUtc = DateTime.UtcNow }
                };
            });
        }

        // Setup batch load mock
        playerServiceMock.Setup(ps => ps.LoadDataBatchAsync(It.IsAny<IEnumerable<int>>()))
            .Returns<IEnumerable<int>>(ids =>
            {
                Thread.Sleep(ioDelayMs); // Simulate single batch IO delay
                return Task.FromResult(ids.Select(id => new PlayerSaveData
                {
                    Character = new ServerCharacterData { Id = id, Name = $"Member_{id}", Level = 1, LastLoginUtc = DateTime.UtcNow }
                }));
            });

        var rosterService = new GuildRosterService(guildManager, playerServiceMock.Object);

        _output.WriteLine($"Starting optimized benchmark: {memberCount} members, {memberCount - 1} offline, {ioDelayMs}ms delay for ONE batch load");

        var sw = Stopwatch.StartNew();
        await rosterService.SendFullRosterAsync(requesterId);
        sw.Stop();

        _output.WriteLine($"SendFullRoster optimized time: {sw.ElapsedMilliseconds}ms");

        // Expected time is around ioDelayMs, definitely much less than (memberCount - 1) * ioDelayMs
        Assert.True(sw.ElapsedMilliseconds < (memberCount - 1) * ioDelayMs, $"Expected less than {(memberCount - 1) * ioDelayMs}ms, but got {sw.ElapsedMilliseconds}ms");
        Assert.True(sw.ElapsedMilliseconds >= ioDelayMs, $"Expected at least {ioDelayMs}ms, but got {sw.ElapsedMilliseconds}ms");
    }
}
