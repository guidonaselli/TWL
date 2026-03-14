# T01: 03-content-quality 01

**Slice:** S03 — **Milestone:** M001

## Description

Stabilize Hidden Ruins and Ruins Expansion content so quest chains are internally consistent and regression-safe.

Purpose: This plan covers the highest-risk content branch in QUAL-01 by making quest data and interaction behavior explicit, test-backed, and resilient to drift.
Output: Updated quest/interaction content for the 1301-1307 branch plus stronger targeted tests and validator checks.

## Must-Haves

- [ ] "Hidden Ruins and Ruins Expansion quest chains progress from start to reward claim without blockers"
- [ ] "Quest prerequisites and objective target names for quests 1301-1307 remain internally consistent"
- [ ] "Arc regressions fail fast through deterministic automated tests"

## Files

- `Content/Data/quests.json`
- `Content/Data/interactions.json`
- `TWL.Tests/HiddenRuinsQuestTests.cs`
- `TWL.Tests/QuestRuinsExpansionTests.cs`
- `TWL.Tests/Quests/QuestValidationTests.cs`
