using System;
using System.Linq;
using TWL.Shared.Domain.DTO;
using TWL.Shared.Domain.Guilds;
using TWL.Server.Persistence.Services;

namespace TWL.Server.Simulation.Managers;

public class GuildRosterService
{
    private readonly GuildManager _guildManager;
    private readonly PlayerService _playerService;

    public GuildRosterService(GuildManager guildManager, PlayerService playerService)
    {
        _guildManager = guildManager;
        _playerService = playerService;
    }

    public async Task SendFullRosterAsync(int memberId)
    {
        var guild = _guildManager.GetGuildByMember(memberId);
        if (guild == null) return;

        var session = _playerService.GetSession(memberId);
        if (session == null) return;

        var syncEvent = new GuildRosterSyncEvent();

        int[] members;
        lock (guild.MemberIds)
        {
            members = guild.MemberIds.ToArray();
        }

        // Optimization: Identify offline members for batch loading
        var offlineMemberIds = new List<int>();
        var memberDtos = new Dictionary<int, GuildMemberDto>();

        foreach (var pId in members)
        {
            var pSession = _playerService.GetSession(pId);
            if (pSession != null && pSession.Character != null)
            {
                memberDtos[pId] = new GuildMemberDto
                {
                    CharacterId = pId,
                    Name = pSession.Character.Name,
                    Level = pSession.Character.Level,
                    IsOnline = true,
                    Rank = guild.MemberRanks.GetValueOrDefault(pId, GuildRank.Recruit),
                    LastLoginUtc = pSession.Character.LastLoginUtc
                };
            }
            else
            {
                offlineMemberIds.Add(pId);
            }
        }

        if (offlineMemberIds.Count > 0)
        {
            var offlineData = await _playerService.LoadDataBatchAsync(offlineMemberIds);
            foreach (var data in offlineData)
            {
                memberDtos[data.Character.Id] = new GuildMemberDto
                {
                    CharacterId = data.Character.Id,
                    Name = data.Character.Name,
                    Level = data.Character.Level,
                    IsOnline = false,
                    Rank = guild.MemberRanks.GetValueOrDefault(data.Character.Id, GuildRank.Recruit),
                    LastLoginUtc = data.Character.LastLoginUtc
                };
            }
        }

        // Fill in defaults for any members not found in DB
        foreach (var pId in members)
        {
            if (!memberDtos.TryGetValue(pId, out var dto))
            {
                dto = new GuildMemberDto
                {
                    CharacterId = pId,
                    Name = "Unknown",
                    Level = 1,
                    IsOnline = false,
                    Rank = guild.MemberRanks.GetValueOrDefault(pId, GuildRank.Recruit),
                    LastLoginUtc = DateTime.UtcNow
                };
            }
            syncEvent.Members.Add(dto);
        }

        var msg = new TWL.Shared.Net.Network.NetMessage
        {
            Op = TWL.Shared.Net.Network.Opcode.GuildRosterSync,
            JsonPayload = System.Text.Json.JsonSerializer.Serialize(syncEvent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        };
        await session.SendAsync(msg);
    }

    public async Task BroadcastRosterUpdateAsync(int guildId, int targetMemberId, bool isRemoved = false)
    {
        var guild = _guildManager.GetGuild(guildId);
        if (guild == null) return;

        GuildMemberDto memberDto;
        if (isRemoved)
        {
            memberDto = new GuildMemberDto { CharacterId = targetMemberId };
        }
        else
        {
            var session = _playerService.GetSession(targetMemberId);
            if (session != null && session.Character != null)
            {
                memberDto = new GuildMemberDto
                {
                    CharacterId = targetMemberId,
                    Name = session.Character.Name,
                    Level = session.Character.Level,
                    IsOnline = true,
                    Rank = guild.MemberRanks.GetValueOrDefault(targetMemberId, GuildRank.Recruit),
                    LastLoginUtc = session.Character.LastLoginUtc
                };
            }
            else
            {
                var saveData = await _playerService.LoadDataAsync(targetMemberId);
                memberDto = new GuildMemberDto
                {
                    CharacterId = targetMemberId,
                    Name = saveData?.Character.Name ?? "Unknown",
                    Level = saveData?.Character.Level ?? 1,
                    IsOnline = false,
                    Rank = guild.MemberRanks.GetValueOrDefault(targetMemberId, GuildRank.Recruit),
                    LastLoginUtc = saveData?.Character.LastLoginUtc ?? DateTime.UtcNow
                };
            }
        }

        var updateEvent = new GuildRosterUpdateEvent
        {
            Member = memberDto,
            IsRemoved = isRemoved
        };

        var msg = new TWL.Shared.Net.Network.NetMessage
        {
            Op = TWL.Shared.Net.Network.Opcode.GuildRosterUpdate,
            JsonPayload = System.Text.Json.JsonSerializer.Serialize(updateEvent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        };

        int[] members;
        lock (guild.MemberIds)
        {
            members = guild.MemberIds.ToArray();
        }

        foreach (var pId in members)
        {
            var session = _playerService.GetSession(pId);
            if (session != null)
            {
                _ = session.SendAsync(msg);
            }
        }
    }

    public async Task BroadcastMemberPresenceUpdateAsync(int memberId, bool isOnline)
    {
        var guild = _guildManager.GetGuildByMember(memberId);
        if (guild == null) return;

        await BroadcastRosterUpdateAsync(guild.GuildId, memberId, false);
    }
}
