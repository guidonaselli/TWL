# Quality Guardian Execution Plan

1. **Step 1: Build Check**
   - Result: Successful
2. **Step 2: Full Test Suite**
   - Result: 807 total, 801 passed, 1 failed, 5 skipped
   - Failed test: `TWL.Tests.Performance.GuildRosterPerformanceTests.Benchmark_SendFullRoster_Optimized`
   - Test class: `TWL.Tests.Performance.GuildRosterPerformanceTests`
   - Error: `System.NotSupportedException : Unsupported expression: ps => ps.LoadData(id). Non-overridable members (here: PlayerService.LoadData) may not be used in setup / verification expressions.`
   - Category: New. The test was added in commit `0d4b6db030af7ffcf1f0c7d9156772b7f3699efd` on Mar 20.
3. **Step 3: Content Validation**
   - Result: Successful. Passed 25, Failed 0. No issues.
4. **Step 4: Branch Health**
   - Scanned `origin/jules/*` and `origin/gsd/*`.
   - Identified stale branches (older than 14 days from Mar 20):
     - `origin/jules/reliability-world-loop-7335788999048971022` (2026-01-29)
     - `origin/jules/vertical-slice-completion-7696239346302303360` (2026-01-18)
5. **Step 5: Write Report**
   - Path: `reports/jules/daily-quality/2026-03-20.md`
   - Contents following the provided template.
6. Complete pre commit steps to ensure proper testing, verification, review, and reflection are done.
7. **Submit**
   - Create commit: `chore: daily quality report 2026-03-20`
   - Submit.
