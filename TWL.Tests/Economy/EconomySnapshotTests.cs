using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Models;
using Xunit;

namespace TWL.Tests.Economy;

public class EconomySnapshotTests
{
    [Fact]
    public void Snapshot_RestoresState_And_SkipsOldLedger()
    {
        var ledgerFile = $"snap_ledger_{Guid.NewGuid():N}.log";
        var snapshotFile = Path.ChangeExtension(ledgerFile, ".snapshot.json");
        var opId1 = "op_1";
        var opId2 = "op_2";
        var opId3 = "op_3";

        try
        {
            // 1. Initial Session
            using (var manager1 = new EconomyManager(ledgerFile))
            {
                var char1 = new ServerCharacter { Id = 1, PremiumCurrency = 100 };

                // Transaction A
                manager1.BuyShopItem(char1, 1, 1, opId1); // Cost 10

                // Transaction B
                manager1.BuyShopItem(char1, 1, 1, opId2); // Cost 10

                Assert.Equal(2, manager1.Metrics.ActiveTransactions);

                // manager1.Dispose() will be called at end of block, saving snapshot.
            }

            Assert.True(File.Exists(snapshotFile), "Snapshot file should exist after Dispose");

            // 2. Simulate "Crash" and Append new log manually or via another manager
            // We'll use a temporary manager just to write to the log, but we need to trick it to NOT overwrite snapshot?
            // Actually, if we just append to the file using StreamWriter, it mimics the logger.
            // But easier: Create manager2, it loads snapshot, adds transaction C, then we inspect metrics.

            // Wait a bit to ensure timestamps differ if needed (though we use <= comparison)
            // But we want to ensure C is AFTER snapshot.

            using (var manager2 = new EconomyManager(ledgerFile))
            {
                // It should have loaded 2 transactions from snapshot.
                Assert.Equal(2, manager2.Metrics.ActiveTransactions);
                // Replay count should be 0 because ledger contains timestamps <= snapshot timestamp?
                // Actually, snapshot timestamp is set when SaveSnapshot is called.
                // Ledger entries were written BEFORE that. So ReplayLedger should skip them.
                Assert.Equal(0, manager2.Metrics.ReplayedTransactionCount);

                var char1 = new ServerCharacter { Id = 1, PremiumCurrency = 80 }; // 100 - 10 - 10

                // Transaction C
                manager2.BuyShopItem(char1, 1, 1, opId3); // Cost 10

                Assert.Equal(3, manager2.Metrics.ActiveTransactions);
            }

            // 3. Third session: Should load Snapshot (updated by manager2?)
            // Wait, manager2 also saved snapshot on dispose!
            // So manager3 should load 3 transactions from snapshot, 0 from ledger replay.

            using (var manager3 = new EconomyManager(ledgerFile))
            {
                Assert.Equal(3, manager3.Metrics.ActiveTransactions);
                Assert.Equal(0, manager3.Metrics.ReplayedTransactionCount);
            }

            // 4. Test "Partial Replay": What if snapshot is old?
            // Delete snapshot, keep ledger.
            File.Delete(snapshotFile);

            using (var manager4 = new EconomyManager(ledgerFile))
            {
                // Should replay all 3 from ledger
                Assert.Equal(3, manager4.Metrics.ActiveTransactions);
                Assert.Equal(3, manager4.Metrics.ReplayedTransactionCount);
            }
        }
        finally
        {
            if (File.Exists(ledgerFile)) File.Delete(ledgerFile);
            if (File.Exists(snapshotFile)) File.Delete(snapshotFile);
        }
    }
}
