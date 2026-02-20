# Weekly — Consistency Auditor Report (Skills ↔ Quests)

## 1) RESULT: REPORT

## 2) SUMMARY
An automated audit was performed on `Content/Data/skills.json` and all `Content/Data/quests*.json` files. The audit checked for broken references (Quest rewards -> Skill IDs), duplicate IDs, inconsistent gating, missing mandatory fields, and logical contradictions. The audit found **7 violations** related to missing mandatory fields (`Requirements`) in quest definitions. No broken references or duplicate skill grants were found. Due to the active daily task `[PERS-001b]`, this audit produces a REPORT instead of a PR, adhering to the Anti-Collision Clause.

## 3) VIOLATIONS (prioritized)

### P2 (Structure) - Missing Mandatory Field: `Requirements`
The following quests are missing the `Requirements` field, which is mandatory according to the schema rules.

- **Content/Data/quests_messenger.json**
  - Quest ID: `5001` (Title: "Speed Training")

- **Content/Data/quests_scenario_demo.json**
  - Quest ID: `99800` (Title: "Slime Hunter")
  - Quest ID: `99801` (Title: "Herb Collector")
  - Quest ID: `99803` (Title: "Join Faction A")
  - Quest ID: `99804` (Title: "Join Faction B")
  - Quest ID: `99805` (Title: "Cave of Trials")
  - Quest ID: `99806` (Title: "Escort VIP")

## 4) PROPOSED FIX
Update the identified quest JSON objects to include the missing `Requirements` field with an empty list `[]` as the default value if no requirements exist.

Example Fix for Quest 5001:
```json
<<<<<<< SEARCH
    "QuestId": 5001,
    "Title": "Speed Training",
    "Description": "Talk to the Messenger to begin your training.",
    "Objectives": [
=======
    "QuestId": 5001,
    "Title": "Speed Training",
    "Description": "Talk to the Messenger to begin your training.",
    "Requirements": [],
    "Objectives": [
>>>>>>> REPLACE
```

## 5) ACTION ITEMS
1.  **Ticket**: Create a task to batch-fix missing `Requirements` fields in `quests_messenger.json` and `quests_scenario_demo.json`.
2.  **Validation**: Verify if `quests_scenario_demo.json` is intended for production or testing; if testing, consider relaxing validation or explicitly marking it as test data.
3.  **Process**: Update the `ContentValidationTests` suite to catch missing mandatory fields in CI.
