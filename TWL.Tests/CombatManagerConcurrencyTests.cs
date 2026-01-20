using Xunit;
using TWL.Server.Simulation.Managers;
using TWL.Server.Simulation.Networking;
using TWL.Shared.Domain.Requests;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using Xunit.Abstractions;

namespace TWL.Tests;

public class CombatManagerConcurrencyTests
{
    private readonly ITestOutputHelper _output;

    public CombatManagerConcurrencyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CombatManager_ConcurrentAccess_ShouldNotCrash()
    {
        var manager = new CombatManager();
        bool running = true;
        var exceptions = new List<Exception>();

        // Setup initial characters
        manager.AddCharacter(new ServerCharacter { Id = 1, Name = "Player1", Hp = 10000, Str = 10 });
        manager.AddCharacter(new ServerCharacter { Id = 2, Name = "Player2", Hp = 10000, Str = 10 });

        var tasks = new List<Task>();

        // Task 1: UseSkill loop (Readers)
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    while (running)
                    {
                        manager.UseSkill(new UseSkillRequest { PlayerId = 1, TargetId = 2 });
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                    running = false;
                }
            }));
        }

        // Task 3: Enumeration loop
        tasks.Add(Task.Run(() =>
        {
            try
            {
                while (running)
                {
                    var chars = manager.GetAllCharacters();
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
                running = false;
            }
        }));

        // Task 2: Add/Remove loop (Writers)
        for (int i = 0; i < 10; i++)
        {
            int tIndex = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    int idCounter = 100 + (tIndex * 10000);
                    while (running)
                    {
                        var id = idCounter++;
                        manager.AddCharacter(new ServerCharacter { Id = id, Name = $"Mob{id}", Hp = 50, Str = 5 });
                        manager.RemoveCharacter(id);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                    running = false;
                }
            }));
        }

        // Let it run
        Thread.Sleep(3000);
        running = false;
        Task.WaitAll(tasks.ToArray());

        foreach (var ex in exceptions)
        {
            _output.WriteLine(ex.ToString());
        }

        Assert.Empty(exceptions);
    }
}
