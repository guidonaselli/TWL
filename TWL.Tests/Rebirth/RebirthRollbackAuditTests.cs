using Moq;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.DTO;
using Microsoft.Extensions.Logging;
using Xunit;
using TWL.Server.Simulation.Networking.Components;
using TWL.Shared.Domain.Models;
using System.Linq;
using System;
using System.Collections.Generic;

namespace TWL.Tests.Rebirth;

/// <summary>
/// Failure-path tests for Rebirth rollback and audit behavior.
/// </summary>
public class RebirthRollbackAuditTests
{
    private class FaultyCharacter : ServerCharacter
    {
        public bool ShouldFailReset { get; set; }

        public override void ResetStatsToBaseline()
        {
            if (ShouldFailReset)
            {
                throw new InvalidOperationException("Simulated database/logic failure during stat reset.");
            }
            base.ResetStatsToBaseline();
        }
    }

    private class ThrowingList<T> : IList<T>
    {
        private readonly List<T> _inner = new();
        public int FailAtCount { get; set; } = -1;
        private int _addCount = 0;

        public void Add(T item)
        {
            if (_addCount == FailAtCount)
            {
                _addCount++;
                throw new InvalidOperationException("Simulated Persistence Failure");
            }
            _addCount++;
            _inner.Add(item);
        }

        public int Count => _inner.Count;
        public bool IsReadOnly => false;
        public T this[int index] { get => _inner[index]; set => _inner[index] = value; }
        public void Clear() => _inner.Clear();
        public bool Contains(T item) => _inner.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        public int IndexOf(T item) => _inner.IndexOf(item);
        public void Insert(int index, T item) => _inner.Insert(index, item);
        public bool Remove(T item) => _inner.Remove(item);
        public void RemoveAt(int index) => _inner.RemoveAt(index);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();
    }

    private readonly Mock<ILogger<RebirthManager>> _mockLogger;
    private readonly RebirthManager _rebirthManager;

    public RebirthRollbackAuditTests()
    {
        _mockLogger = new Mock<ILogger<RebirthManager>>();
        _rebirthManager = new RebirthManager(_mockLogger.Object);
    }

    [Fact]
    public void Rebirth_ShouldRollback_OnLogicException()
    {
        // Setup
        var character = new FaultyCharacter
        {
            Id = 1,
            Level = 100,
            RebirthLevel = 0,
            StatPoints = 10,
            ShouldFailReset = true
        };
        character.QuestComponent = new PlayerQuestComponent(new ServerQuestManager());
        character.QuestComponent.Character = character;
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        character.AddItem(9007, 1);

        // Act
        var (success, message, _) = _rebirthManager.TryRebirthCharacter(character, "fail-op-1");

        // Assert
        Assert.False(success);
        Assert.Contains("Internal error", message);

        // Verify Rollback
        Assert.Equal(100, character.Level);
        Assert.Equal(0, character.RebirthLevel);
        Assert.Equal(10, character.StatPoints);
        
        // Item should NOT be consumed if transaction failed
        Assert.True(character.HasItem(9007, 1));
    }

    [Fact]
    public void Rebirth_ShouldRollback_OnPersistenceException()
    {
        // Arrange
        var history = new ThrowingList<RebirthHistoryRecord>();
        var character = new ServerCharacter
        {
            Id = 1,
            Level = 100,
            RebirthLevel = 0,
            StatPoints = 0,
            RebirthHistory = history
        };
        character.QuestComponent = new PlayerQuestComponent(new ServerQuestManager());
        character.QuestComponent.Character = character;
        character.QuestComponent.AddFlag("REBIRTH_QUALIFIED");
        character.AddItem(9007, 1);

        _rebirthManager.SetRequirements(new RebirthRequirements { MinLevel = 100 });
        
        // Fail at the first Add (which is the success record in the Try block)
        history.FailAtCount = 0;
        string opId = "ROLLBACK_TEST";

        // Act
        var (success, msg, points) = _rebirthManager.TryRebirthCharacter(character, opId);

        // Assert
        Assert.False(success);
        
        // Verify state is rolled back
        Assert.Equal(100, character.Level);
        Assert.Equal(0, character.RebirthLevel);
        Assert.Equal(0, character.StatPoints);

        // Verify that a FAILURE record was recorded (added by LogAndRecordFailure in catch block)
        Assert.Single(history);
        Assert.False(history[0].Success);
        Assert.Contains("Transaction failure", history[0].Reason);
    }

    [Fact]
    public void Audit_ShouldLogFailure_WhenRequirementsMissing()
    {
        var character = new ServerCharacter { Id = 1, Level = 99 }; // Too low
        
        var (success, message, _) = _rebirthManager.TryRebirthCharacter(character, "audit-fail-1");
        
        Assert.False(success);
        Assert.Contains("Level 100 required", message);

        // Verify Logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Rebirth failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
