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