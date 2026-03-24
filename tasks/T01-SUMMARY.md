# T01 Summary: Finalize spawn tables for Regions 1 and 2

**Content Created:**
- Created 13 spawn configuration files mapping `ZoneSpawnConfig` objects to Map IDs.

**Counts & Coverage:**
- **Isla Brisa (Region 1, Maps 1-8):** 8 total spawn zones populated with Level 1-10 monsters.
  - Monsters Used: Dirt Slime (2013), Root Vine (2009), Coast Crab (2001), Aqua Slime (2014), Kelp Vine (2010), Tide Crab (2002), Magma Crab (2003), Cave Bat (9101), Zephyr Crab (2004), Flame Slime (2015), Dirt Bat (2017), Stone Guardian (3001), Rock Monkey (2005), River Monkey (2006), Ember Monkey (2007), Cloud Monkey (2008), Gale Slime (2016), Aqua Bat (2018), Flame Bat (2019).
  - Boss: Isla Brisa Boss (2069).
- **Puerto Roca (Region 2, Maps 1000-1003, 1010):** 5 total spawn zones populated with Level 11-20 monsters.
  - Monsters Used: Gale Bat (2020), Dirt Wolf (2021), Aqua Wolf (2022), Flame Wolf (2023), Gale Wolf (2024), Dirt Bear (2025), Aqua Bear (2026).
  - Boss: Puerto Roca Boss (2070).

**Verification Note:**
Validation via `dotnet test TWL.Tests/TWL.Tests.csproj --filter ContentValidationTests` was skipped because upstream C# compilation errors (`chara does not exist in the current context` in `TWL.Server/Services/PetService.cs`) blocked the test runner from compiling the solution. Per Content Creator Role Constraints, the test has been bypassed without modifying the underlying C# codebase.
