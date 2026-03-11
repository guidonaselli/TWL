using System;
using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Shared.Domain.DTO;

namespace TWL.Tests.Guild
{
    public class GuildLifecycleTests
    {
        private readonly GuildManager _guildManager;

        public GuildLifecycleTests()
        {
            _guildManager = new GuildManager();
        }

        [Fact]
        public void CreateGuild_Success_ReturnsTrueAndSetsGuild()
        {
            // Act
            var result = _guildManager.CreateGuild(1, "Leader", "Test Guild");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Guild created successfully.", result.Message);

            var guild = _guildManager.GetGuildByMember(1);
            Assert.NotNull(guild);
            Assert.Equal("Test Guild", guild.Name);
            Assert.Equal(1, guild.LeaderId);
            Assert.Single(guild.MemberIds);
            Assert.Contains(1, guild.MemberIds);
        }

        [Fact]
        public void CreateGuild_DuplicateName_ReturnsFalse()
        {
            // Arrange
            _guildManager.CreateGuild(1, "Leader1", "Test Guild");

            // Act
            var result = _guildManager.CreateGuild(2, "Leader2", "Test Guild");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Guild name is already taken.", result.Message);
        }

        [Fact]
        public void InviteMember_Success_ReturnsTrue()
        {
            // Arrange
            _guildManager.CreateGuild(1, "Leader", "Test Guild");

            // Act
            var result = _guildManager.InviteMember(1, "Leader", 2, "Target");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Invite sent.", result.Message);
        }

        [Fact]
        public void InviteMember_TargetAlreadyInGuild_ReturnsFalse()
        {
            // Arrange
            _guildManager.CreateGuild(1, "Leader1", "Test Guild 1");
            _guildManager.CreateGuild(2, "Leader2", "Test Guild 2");

            // Act
            var result = _guildManager.InviteMember(1, "Leader1", 2, "Leader2");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Target player is already in a guild.", result.Message);
        }

        [Fact]
        public void AcceptInvite_Success_AddsMember()
        {
            // Arrange
            _guildManager.CreateGuild(1, "Leader", "Test Guild");
            var guild = _guildManager.GetGuildByMember(1);
            Assert.NotNull(guild);
            _guildManager.InviteMember(1, "Leader", 2, "Target");

            // Act
            var result = _guildManager.AcceptInvite(2, guild.GuildId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, guild.MemberIds.Count);
            Assert.Contains(2, guild.MemberIds);
            Assert.Equal(guild.GuildId, _guildManager.GetGuildByMember(2)?.GuildId);
        }

        [Fact]
        public void DeclineInvite_Success_RemovesInvite()
        {
            // Arrange
            _guildManager.CreateGuild(1, "Leader", "Test Guild");
            var guild = _guildManager.GetGuildByMember(1);
            Assert.NotNull(guild);
            _guildManager.InviteMember(1, "Leader", 2, "Target");

            // Act
            var result = _guildManager.DeclineInvite(2, guild.GuildId);

            // Assert
            Assert.True(result);
            var acceptResult = _guildManager.AcceptInvite(2, guild.GuildId);
            Assert.False(acceptResult.Success);
            Assert.Equal("No active invite found for this guild.", acceptResult.Message);
        }

        [Fact]
        public void LeaveGuild_Success_RemovesMemberAndDisbandsIfEmpty()
        {
            // Arrange
            _guildManager.CreateGuild(1, "Leader", "Test Guild");
            var guild = _guildManager.GetGuildByMember(1);
            Assert.NotNull(guild);
            var guildId = guild.GuildId;

            // Act
            var result = _guildManager.LeaveGuild(1);

            // Assert
            Assert.True(result);
            Assert.Null(_guildManager.GetGuildByMember(1));
            Assert.Null(_guildManager.GetGuild(guildId)); // Guild should be disbanded
        }

        [Fact]
        public void LeaveGuild_LeaderLeavesWithOtherMembers_AssignsNewLeader()
        {
            // Arrange
            _guildManager.CreateGuild(1, "Leader", "Test Guild");
            var guild = _guildManager.GetGuildByMember(1);
            Assert.NotNull(guild);
            _guildManager.InviteMember(1, "Leader", 2, "Target");
            _guildManager.AcceptInvite(2, guild.GuildId);

            // Act
            var result = _guildManager.LeaveGuild(1);

            // Assert
            Assert.True(result);
            Assert.Null(_guildManager.GetGuildByMember(1));
            Assert.NotNull(_guildManager.GetGuild(guild.GuildId));
            Assert.Equal(2, guild.LeaderId);
            Assert.Single(guild.MemberIds);
            Assert.Contains(2, guild.MemberIds);
        }

        [Fact]
        public void KickMember_LeaderKicksMember_Success()
        {
            // Arrange
            _guildManager.CreateGuild(1, "Leader", "Test Guild");
            var guild = _guildManager.GetGuildByMember(1);
            Assert.NotNull(guild);
            _guildManager.InviteMember(1, "Leader", 2, "Target");
            _guildManager.AcceptInvite(2, guild.GuildId);

            // Act
            var result = _guildManager.KickMember(1, 2);

            // Assert
            Assert.True(result.Success);
            Assert.Null(_guildManager.GetGuildByMember(2));
            Assert.Single(guild.MemberIds);
            Assert.Contains(1, guild.MemberIds);
        }

        [Fact]
        public void KickMember_NonLeaderKicksMember_ReturnsFalse()
        {
            // Arrange
            _guildManager.CreateGuild(1, "Leader", "Test Guild");
            var guild = _guildManager.GetGuildByMember(1);
            Assert.NotNull(guild);
            _guildManager.InviteMember(1, "Leader", 2, "Target1");
            _guildManager.AcceptInvite(2, guild.GuildId);
            _guildManager.InviteMember(1, "Leader", 3, "Target2");
            _guildManager.AcceptInvite(3, guild.GuildId);

            // Act
            var result = _guildManager.KickMember(2, 3); // Member 2 tries to kick Member 3

            // Assert
            Assert.False(result.Success);
            Assert.Equal("You do not have permission to kick this member.", result.Message);
            Assert.NotNull(_guildManager.GetGuildByMember(3));
            Assert.Equal(3, guild.MemberIds.Count);
        }
    }
}
