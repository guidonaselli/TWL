1) TITLE: Implementar Protocol Schema Validation (CORE-001)
2) TYPE: REPORT
3) SCOPE (IN):
- TWL.Shared/Constants/ProtocolConstants.cs
- TWL.Shared/Net/Network/NetMessage.cs
- TWL.Server/Simulation/Networking/ClientSession.cs
- TWL.Client/Net/NetworkClient.cs
- TWL.Tests/Security/SecurityTests.cs y actualizaciones en otros tests
4) OUT-OF-SCOPE:
- Modificaciones en la lógica interna de ReplayGuard o RateLimiter.
- Modificaciones en los handlers específicos de Opcodes.
- Sistemas de gameplay o contenido (quests, pets, etc.).
5) ACCEPTANCE CRITERIA (DoD):
- `NetMessage` incluye una propiedad `int? SchemaVersion`.
- `ClientSession.HandleMessageAsync` valida `SchemaVersion` en orden estricto: 1) ReplayGuard, 2) RateLimiter, 3) SchemaVersion, 4) Opcode Dispatch.
- Si `SchemaVersion` es null o distinto a `ProtocolConstants.CurrentSchemaVersion`, se desconecta la sesión inmediatamente (Fail-Closed).
- `NetworkClient` inyecta automáticamente `CurrentSchemaVersion` en los paquetes salientes antes de su envío.
6) REQUIRED TESTS / VALIDATIONS:
- Implementar `SecurityTests.Connect_WithInvalidSchema_ShouldDisconnect` y verificar que pase.
- Todos los tests existentes que instancian `NetMessage` deben ser actualizados asignando explícitamente `SchemaVersion = ProtocolConstants.CurrentSchemaVersion`.
7) RISKS:
- Riesgo: Fallo generalizado en la suite de tests por rechazos en ClientSession. Mitigación: Revisar y actualizar todas las instancias manuales de NetMessage en TWL.Tests.
- Riesgo: Desconexión inadvertida del cliente. Mitigación: Centralizar la inyección automática en el método base `NetworkClient.SendNetMessage`.
8) NEXT: CORE-002: Sesiones + sequence/nonce por cliente para protección anti-replay.