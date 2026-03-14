# T02: 03-content-quality 02

**Slice:** S03 — **Milestone:** M001

## Description

Stabilize Hidden Cove chain content and interaction contracts to prevent progression and crafting regressions.

Purpose: This plan completes the second high-risk branch in QUAL-01 and ensures cove-specific content remains executable end-to-end.
Output: Updated hidden-cove quest and interaction data with stronger regression checks for 1401-1404 and 2401.

## Must-Haves

- [ ] "Hidden Cove main chain (1401-1404) progresses without dead-ends and claims rewards correctly"
- [ ] "Hidden Cove sidequest (2401) respects prerequisites and interaction-based item acquisition"
- [ ] "Crafting and item-consumption interactions required for cove progression remain reliable"

## Files

- `Content/Data/quests.json`
- `Content/Data/interactions.json`
- `TWL.Tests/HiddenCoveTests.cs`
- `TWL.Tests/Quests/QuestValidationTests.cs`
