using System;
using Xunit;
using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Services;
using TWL.Shared.Domain.DTO;
using TWL.Server.Simulation.Networking;

namespace TWL.Tests.Guild
{
    public class GuildLifecycleTests
    {
        private GuildManager _guildManager;
        private Mock<IEconomyService> _mockEconomy;

        public GuildLifecycleTests()
        {
            _mockEconomy = new Mock<IEconomyService>();

            // Setup default success for gold deduction
            _mockEconomy.Setup(x => x.TryDeductGold(It.IsAny<ServerCharacter>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            _guildManager = new GuildManager(_mockEconomy.Object);
        }

        private ServerCharacter CreateLeader(int id = 1, string name = "Leader", int gold = 100000)
        {
            return new ServerCharacter { Id = id, Name = name, Gold = gold };
        }

        [Fact]
        public void CreateGuild_Success()
        {
            var leader = CreateLeader();
            var response = _guildManager.CreateGuild(leader, "TestGuild", leader.Gold);

            Assert.True(response.Success);
            Assert.True(response.GuildId > 0);

            var guild = _guildManager.GetGuildByMember(leader.Id);
            Assert.NotNull(guild);
            Assert.Equal("TestGuild", guild.GuildName);
            Assert.Equal(leader.Id, guild.LeaderId);
            Assert.Single(guild.MemberIds);
            Assert.Contains(leader.Id, guild.MemberIds);
        }

        [Fact]
        public void CreateGuild_DuplicateName_Rejects()
        {
            var leader1 = CreateLeader(1, "Leader1");
            _guildManager.CreateGuild(leader1, "DuplicateGuild", leader1.Gold);

            var leader2 = CreateLeader(2, "Leader2");
            var response = _guildManager.CreateGuild(leader2, "duplicateguild", leader2.Gold);

            Assert.False(response.Success);
            Assert.Equal("Guild name already exists.", response.Message);
        }

        [Fact]
        public void CreateGuild_InsufficientFee_Rejects()
        {
            var leader = CreateLeader(1, "Leader", (int)GuildManager.CreationFee - 1);
            var response = _guildManager.CreateGuild(leader, "PoorGuild", leader.Gold);

            Assert.False(response.Success);
            Assert.Equal("Insufficient gold to create a guild.", response.Message);
            Assert.Null(_guildManager.GetGuildByMember(leader.Id));
        }

        [Fact]
        public void Invite_Accept_Success()
        {
            var leader = CreateLeader();
            _guildManager.CreateGuild(leader, "InviteGuild", leader.Gold);

            var inviteResponse = _guildManager.InviteMember(leader.Id, leader.Name, 2, "Target");
            Assert.True(inviteResponse.Success);

            var acceptResult = _guildManager.AcceptInvite(2, leader.Id);
            Assert.True(acceptResult);

            var guild = _guildManager.GetGuildByMember(2);
            Assert.NotNull(guild);
            Assert.Equal(2, guild.MemberIds.Count);
            Assert.Contains(2, guild.MemberIds);
        }

        [Fact]
        public void Invite_Decline_Success()
        {
            var leader = CreateLeader();
            _guildManager.CreateGuild(leader, "DeclineGuild", leader.Gold);

            _guildManager.InviteMember(leader.Id, leader.Name, 2, "Target");

            var declineResult = _guildManager.DeclineInvite(2, leader.Id);
            Assert.True(declineResult);

            var guild = _guildManager.GetGuildByMember(leader.Id);
            Assert.Single(guild!.MemberIds);
            Assert.DoesNotContain(2, guild.MemberIds);

            var acceptLater = _guildManager.AcceptInvite(2, leader.Id);
            Assert.False(acceptLater);
        }

        [Fact]
        public void LeaveGuild_Success()
        {
            var leader = CreateLeader();
            _guildManager.CreateGuild(leader, "LeaveGuild", leader.Gold);
            _guildManager.InviteMember(leader.Id, leader.Name, 2, "Target");
            _guildManager.AcceptInvite(2, leader.Id);

            var leaveResult = _guildManager.LeaveGuild(2);
            Assert.True(leaveResult);

            var guild = _guildManager.GetGuildByMember(leader.Id);
            Assert.Single(guild!.MemberIds);
            Assert.DoesNotContain(2, guild.MemberIds);
            Assert.Null(_guildManager.GetGuildByMember(2));
        }

        [Fact]
        public void KickMember_Authorized_Success()
        {
            var leader = CreateLeader();
            _guildManager.CreateGuild(leader, "KickGuild", leader.Gold);
            _guildManager.InviteMember(leader.Id, leader.Name, 2, "Target");
            _guildManager.AcceptInvite(2, leader.Id);

            var kickResponse = _guildManager.KickMember(leader.Id, 2);
            Assert.True(kickResponse.Success);

            var guild = _guildManager.GetGuildByMember(leader.Id);
            Assert.Single(guild!.MemberIds);
            Assert.DoesNotContain(2, guild.MemberIds);
            Assert.Null(_guildManager.GetGuildByMember(2));
        }

        [Fact]
        public void KickMember_Unauthorized_Rejects()
        {
            var leader = CreateLeader();
            _guildManager.CreateGuild(leader, "UnauthorizedKickGuild", leader.Gold);
            _guildManager.InviteMember(leader.Id, leader.Name, 2, "Target");
            _guildManager.AcceptInvite(2, leader.Id);
            _guildManager.InviteMember(leader.Id, leader.Name, 3, "Target3");
            _guildManager.AcceptInvite(3, leader.Id);

            // Member 2 tries to kick Member 3
            var kickResponse = _guildManager.KickMember(2, 3);
            Assert.False(kickResponse.Success);
            Assert.Equal("You do not have permission to kick.", kickResponse.Message);

            var guild = _guildManager.GetGuildByMember(leader.Id);
            Assert.Contains(3, guild!.MemberIds);
        }

        [Fact]
        public void KickMember_TargetNotInGuild_Rejects()
        {
            var leader = CreateLeader();
            _guildManager.CreateGuild(leader, "KickMissingGuild", leader.Gold);

            var kickResponse = _guildManager.KickMember(leader.Id, 999);
            Assert.False(kickResponse.Success);
            Assert.Equal("Target not found in your guild.", kickResponse.Message);
        }
    }
}
