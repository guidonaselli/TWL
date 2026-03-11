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

    public void SendFullRoster(int memberId)
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

        foreach (var pId in members)
        {
            syncEvent.Members.Add(CreateMemberDto(guild, pId));
        }

        var msg = new TWL.Shared.Net.Network.NetMessage 
        { 
            Op = TWL.Shared.Net.Network.Opcode.GuildRosterSync, 
            JsonPayload = System.Text.Json.JsonSerializer.Serialize(syncEvent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        };
        _ = session.SendAsync(msg);
    }

    public void BroadcastRosterUpdate(int guildId, int targetMemberId, bool isRemoved = false)
    {
        var guild = _guildManager.GetGuild(guildId);
        if (guild == null) return;

        var updateEvent = new GuildRosterUpdateEvent
        {
            Member = isRemoved ? new GuildMemberDto { CharacterId = targetMemberId } : CreateMemberDto(guild, targetMemberId),
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

    public void BroadcastMemberPresenceUpdate(int memberId, bool isOnline)
    {
        var guild = _guildManager.GetGuildByMember(memberId);
        if (guild == null) return;

        BroadcastRosterUpdate(guild.GuildId, memberId, false);
    }

    private GuildMemberDto CreateMemberDto(Guild guild, int characterId)
    {
        var session = _playerService.GetSession(characterId);
        var isOnline = session != null && session.Character != null;
        var rank = guild.MemberRanks.GetValueOrDefault(characterId, GuildRank.Recruit);

        string name = "Unknown";
        int level = 1;
        DateTime lastLogin = DateTime.UtcNow;

        if (isOnline)
        {
            name = session!.Character!.Name;
            level = session.Character.Level;
            lastLogin = session.Character.LastLoginUtc;
        }
        else
        {
            // Try to load offline data
            var saveData = _playerService.LoadData(characterId);
            if (saveData != null)
            {
                name = saveData.Character.Name;
                level = saveData.Character.Level;
                lastLogin = saveData.Character.LastLoginUtc;
            }
        }

        return new GuildMemberDto
        {
            CharacterId = characterId,
            Name = name,
            Level = level,
            IsOnline = isOnline,
            Rank = rank,
            LastLoginUtc = lastLogin
        };
    }
}
