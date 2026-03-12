using System.Threading.Tasks;
using System;
using System.Linq;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Guilds;
using TWL.Shared.Domain.Models;

namespace TWL.Tests.Guild
{
    public class GuildStorageTests
    {
        private readonly GuildManager _guildManager;
        private readonly GuildAuditLogService _auditLogService;
        private readonly GuildStorageService _storageService;
        private readonly ServerCharacter _leader;
        private readonly ServerCharacter _recruit;

        public GuildStorageTests()
        {
            _guildManager = new GuildManager(new Mock<TWL.Shared.Domain.Guilds.IGuildRepository>().Object);
            _auditLogService = new GuildAuditLogService();
            _storageService = new GuildStorageService(_guildManager, new Mock<TWL.Shared.Domain.Guilds.IGuildRepository>().Object, _auditLogService, Microsoft.Extensions.Logging.Abstractions.NullLogger<GuildStorageService>.Instance);

            // Set tenure gate to 0 for most tests, we will test it specifically
            _storageService.WithdrawalTenureGate = TimeSpan.Zero;

            _leader = new ServerCharacter { Id = 1, Name = "Leader" };
            _recruit = new ServerCharacter { Id = 2, Name = "Recruit" };

            _guildManager.CreateGuild(_leader.Id, _leader.Name, "Test Guild");
            _guildManager.InviteMember(_leader.Id, _leader.Name, _recruit.Id, _recruit.Name);
            _guildManager.AcceptInvite(_recruit.Id, _guildManager.GetGuildByMember(_leader.Id).GuildId);
        }

        [Fact]
        public async Task DepositItem_ShouldSucceed_AndReflectInStorage()
        {
            // Arrange
            _leader.AddItem(101, 5, BindPolicy.Unbound);

            // Act
            var result = await _storageService.DepositItem(_leader, 101, 3, "op1");

            // Assert
            Assert.True(result.Success);
            var view = await _storageService.ViewStorage(_leader);
            var item = view.Items.FirstOrDefault(i => i.ItemId == 101);
            Assert.NotNull(item);
            Assert.Equal(3, item.Quantity);

            // Verify removal from player
            Assert.True(_leader.HasItem(101, 2));
            Assert.False(_leader.HasItem(101, 3));
        }

        [Fact]
        public async Task WithdrawItem_ShouldSucceed_ForLeader()
        {
            // Arrange
            _leader.AddItem(101, 10, BindPolicy.Unbound);
            await _storageService.DepositItem(_leader, 101, 10, "op1");

            // Act
            var result = await _storageService.WithdrawItem(_leader, 101, 4, "op2");

            // Assert
            Assert.True(result.Success);
            var view = await _storageService.ViewStorage(_leader);
            Assert.Equal(6, view.Items.First(i => i.ItemId == 101).Quantity);
        }

        [Fact]
        public async Task WithdrawItem_ShouldFail_ForRecruit_WithoutPermission()
        {
            // Arrange
            _leader.AddItem(101, 10, BindPolicy.Unbound);
            await _storageService.DepositItem(_leader, 101, 10, "op1");

            // Act
            var result = await _storageService.WithdrawItem(_recruit, 101, 1, "op2");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("You do not have permission to withdraw items.", result.Message);
        }

        [Fact]
        public async Task WithdrawItem_ShouldFail_WhenTenureGateNotMet()
        {
            // Arrange
            _storageService.WithdrawalTenureGate = TimeSpan.FromDays(14);
            _leader.AddItem(101, 10, BindPolicy.Unbound);
            await _storageService.DepositItem(_leader, 101, 10, "op1");

            // Leader has permission (as leader), but also has a join date.
            // In GuildManager.CreateGuild, leader join date is set to Now.

            // Act
            var result = await _storageService.WithdrawItem(_leader, 101, 1, "op2");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("You have not been in the guild long enough to withdraw items.", result.Message);
        }

        [Fact]
        public async Task WithdrawItem_ShouldFail_WhenInsufficientQuantity()
        {
            // Arrange
            _leader.AddItem(101, 5, BindPolicy.Unbound);
            await _storageService.DepositItem(_leader, 101, 5, "op1");

            // Act
            var result = await _storageService.WithdrawItem(_leader, 101, 6, "op2");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Not enough items in storage.", result.Message);
        }

        [Fact]
        public async Task Operation_ShouldBeIdempotent()
        {
            // Act
            _leader.AddItem(101, 5, BindPolicy.Unbound);
            var result1 = await _storageService.DepositItem(_leader, 101, 5, "same-op");
            var result2 = await _storageService.DepositItem(_leader, 101, 5, "same-op");

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);

            var view = await _storageService.ViewStorage(_leader);
            Assert.Equal(5, view.Items.First(i => i.ItemId == 101).Quantity); // Not 10
        }

        [Fact]
        public async Task WithdrawalAttempts_ShouldBeLogged()
        {
            // Arrange
            _leader.AddItem(101, 10, BindPolicy.Unbound);
            await _storageService.DepositItem(_leader, 101, 10, "op1");

            // Act
            await _storageService.WithdrawItem(_leader, 101, 5, "op2"); // Success
            await _storageService.WithdrawItem(_recruit, 101, 1, "op3"); // Fail (Perm)
            await _storageService.WithdrawItem(_leader, 101, 100, "op4"); // Fail (Qty)

            // Assert
            var guild = _guildManager.GetGuildByMember(_leader.Id);
            var logs = _auditLogService.GetAuditLogs(guild.GuildId);

            Assert.Equal(3, logs.Count);
            Assert.Contains(logs, l => l.Success && l.ItemId == 101 && l.Quantity == 5 && l.Reason == "Success");
            Assert.Contains(logs, l => !l.Success && l.Reason == "Permission denied");
            Assert.Contains(logs, l => !l.Success && l.Reason == "Insufficient quantity");
        }
    }
}
