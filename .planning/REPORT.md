1) RESULT: REPORT
2) SUMMARY:
The provided daily task ("Consistency Auditor Report") is missing the mandatory fields: SCOPE, OUT-OF-SCOPE, DoD, and TESTS. As per the strict rules, implementation is blocked until these are provided.
- Missing: SCOPE, OUT-OF-SCOPE, DoD (Acceptance Criteria), and TESTS/VALIDATIONS definitions.
- Options:
  A) Provide the missing definitions so the quest JSON fixes can be implemented.
  B) Discard this task and provide a new properly formatted task.
- Recommendation: Option A. Update the daily task ticket to include the missing fields so the quest ID and narrative fixes can be safely executed without expanding scope.
3) CHANGES:
- None. Implementation was aborted due to the malformed task ticket.
4) VALIDATION:
- No tests were run as no code changes were made. Verified the structure of the existing `.planning/REPORT.md` and confirmed the absence of the required fields.
5) FOLLOW-UPS:
- Update the GSD Orchestrator task ticket with SCOPE, OUT-OF-SCOPE, DoD, and TESTS.
- Re-run the daily execution agent once the ticket is fully compliant.
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
1) TITLE: Implement Protocol Schema Validation (CORE-001)
2) TYPE: REPORT
3) SCOPE (IN):
- `TWL.Shared/Constants/ProtocolConstants.cs`
- `TWL.Shared/Net/Network/NetMessage.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Client/Net/NetworkClient.cs`
- `TWL.Tests/Security/SecurityTests.cs` (y actualización de instanciación en otros tests)
4) OUT-OF-SCOPE:
- Modificación de la lógica interna de `ReplayGuard` o `RateLimiter`.
- Modificación de handlers de Opcodes específicos.
- Cualquier sistema de gameplay o contenido.
5) ACCEPTANCE CRITERIA (DoD):
- `ProtocolConstants.CurrentSchemaVersion` existe en el namespace `TWL.Shared.Constants`.
- `NetMessage` incluye una propiedad nullable `int? SchemaVersion`.
- `ClientSession.HandleMessageAsync` valida `SchemaVersion` en el orden estricto: 1) ReplayGuard, 2) RateLimiter, 3) SchemaVersion, 4) Opcode Dispatch.
- Si `SchemaVersion` es null o no coincide con `ProtocolConstants.CurrentSchemaVersion`, el servidor desconecta la sesión inmediatamente (Fail-Closed).
- `NetworkClient` inyecta automáticamente `CurrentSchemaVersion` en los paquetes salientes antes de su envío.
6) REQUIRED TESTS / VALIDATIONS:
- `SecurityTests.Connect_WithInvalidSchema_ShouldDisconnect` debe implementarse y pasar exitosamente.
- Los tests existentes que instancian `NetMessage` deben pasar (requerirá asignar explícitamente `SchemaVersion = ProtocolConstants.CurrentSchemaVersion` en los tests para evitar rechazos del `ClientSession`).
7) RISKS:
- Riesgo: Caída de la suite de tests por rechazos generalizados de `ClientSession`. Mitigación: Actualizar todas las instancias manuales de `NetMessage` en el proyecto de tests.
- Riesgo: Desconexiones del cliente por olvido de inyección. Mitigación: Centralizar la inyección únicamente en la capa base de `NetworkClient.SendNetMessage` de forma automática.
8) NEXT: PET-003: Wire Pet Capture System (Gameplay P1)
