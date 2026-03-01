1) TITLE: Fix broken item references and logical inconsistencies in Quest rewards
2) TYPE: REPORT
3) SCOPE (IN):
   - Content/Data/quests.json
   - Content/Data/quests_islabrisa_side.json
   - Content/Data/quests_messenger.json
4) OUT-OF-SCOPE:
   - Changes to C# server or client code.
   - Modifications to non-quest JSON data files.
   - Alterations to quest narratives or core logic.
5) ACCEPTANCE CRITERIA (DoD):
   - Quest 1015 rewards Item 6312 (was 8005).
   - Quest 9004 rewards Item 7373 (was 800).
   - Quests 1020 and 1021 reward Item 7316 (was 10001).
   - Quests 2003, 1051, 1100, and 5002 require/reward Item 4739 (was 3001).
   - JSON structure remains perfectly valid.
6) REQUIRED TESTS / VALIDATIONS:
   - `ContentValidationTests` must pass completely to verify schema and data integrity.
7) RISKS:
   - Risk: Breaking JSON formatting. Mitigation: Execute full test suite (`dotnet test`) before submission.
   - Risk: Affecting other quest IDs accidentally. Mitigation: Strict adherence to the scope and DoD items.
8) NEXT: Await explicit authorization to transition to 'PR' and apply the changes.