using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TWL.Server.Persistence.Services;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Net.Network;
using Xunit;
using Xunit.Abstractions;

namespace TWL.Tests.Performance;

public class PartyChatPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PartyChatPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Benchmark_PartyChatBroadcast_Latency()
    {
        var partyId = 1;
        var senderId = 1;
        var memberCount = 4;
        var ioDelayMs = 10;

        var partyServiceMock = new Mock<IPartyService>();
        var playerServiceMock = new Mock<PlayerService>(null, null);
        var loggerMock = new Mock<ILogger<PartyChatService>>();

        var party = new TWL.Server.Simulation.Managers.Party { PartyId = partyId, LeaderId = senderId };
        for (int i = 1; i <= memberCount; i++)
        {
            party.MemberIds.Add(i);
        }

        partyServiceMock.Setup(s => s.GetParty(partyId)).Returns(party);

        var sessions = new List<Mock<ClientSession>>();
        for (int i = 1; i <= memberCount; i++)
        {
            var sessionMock = new Mock<ClientSession>();
            sessionMock.Setup(s => s.SendAsync(It.IsAny<NetMessage>()))
                .Returns(async () => await Task.Delay(ioDelayMs));

            playerServiceMock.Setup(ps => ps.GetSession(i)).Returns(sessionMock.Object);
            sessions.Add(sessionMock);
        }

        var chatService = new PartyChatService(partyServiceMock.Object, playerServiceMock.Object, loggerMock.Object);

        // Warmup
        await chatService.SendPartyMessageAsync(partyId, senderId, "Sender", "Warmup");

        var sw = Stopwatch.StartNew();
        int iterations = 10;
        for (int i = 0; i < iterations; i++)
        {
            await chatService.SendPartyMessageAsync(partyId, senderId, "Sender", $"Message {i}");
        }
        sw.Stop();

        _output.WriteLine($"Total time for {iterations} broadcasts to {memberCount} members (IO delay: {ioDelayMs}ms): {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per broadcast: {sw.ElapsedMilliseconds / (double)iterations}ms");

        // With sequential execution, it should be at least iterations * memberCount * ioDelayMs
        // 10 * 4 * 10 = 400ms.
        // With parallel execution, it should be around iterations * ioDelayMs
        // 10 * 10 = 100ms.
    }
}
