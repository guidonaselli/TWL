using System;
using System.Diagnostics;
using System.Linq;
using TWL.Server.Simulation.Networking;
using Xunit;
using Xunit.Abstractions;

namespace TWL.Tests.Performance
{
    public class KnownSkillsPerfTest
    {
        private readonly ITestOutputHelper _output;

        public KnownSkillsPerfTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BenchmarkKnownSkillsAccess()
        {
            // Setup
            var character = new ServerCharacter();
            // Add 50 skills
            for (int i = 0; i < 50; i++)
            {
                character.LearnSkill(i + 1000);
            }

            int iterations = 100_000;
            var stopwatch = Stopwatch.StartNew();

            long totalCount = 0;
            for (int i = 0; i < iterations; i++)
            {
                // Access property and iterate
                foreach (var skillId in character.KnownSkills)
                {
                    totalCount += skillId;
                }
            }

            stopwatch.Stop();
            _output.WriteLine($"[BENCHMARK] Time taken for {iterations} iterations: {stopwatch.ElapsedMilliseconds} ms");

            // Simple assertion to keep test happy
            Assert.True(totalCount > 0);
        }
    }
}
