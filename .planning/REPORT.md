1) TITLE: Implement Server-Authoritative Protocol Versioning (CORE-001)
2) TYPE: REPORT
3) SCOPE (IN): TWL.Shared.Constants.ProtocolConstants, TWL.Shared.Net.NetMessage, TWL.Server.Networking.ClientSession, TWL.Client.Networking.NetworkClient
4) OUT-OF-SCOPE: Any other gameplay features, Opcode handlers, ReplayGuard or RateLimiter internal logic.
5) ACCEPTANCE CRITERIA (DoD):
   - NetMessage includes `int? SchemaVersion`.
   - ClientSession validates SchemaVersion strictly in this order: ReplayGuard, RateLimiter, SchemaVersion, Opcode Dispatch.
   - Null or mismatched SchemaVersion causes immediate disconnect (Fail-Closed).
   - NetworkClient auto-injects CurrentSchemaVersion into all outgoing messages.
6) REQUIRED TESTS / VALIDATIONS: Unit tests verifying `ClientSession` rejects invalid schema versions. Update all test NetMessage instances to include `SchemaVersion = ProtocolConstants.CurrentSchemaVersion`.
7) RISKS:
   - Widespread test failures due to missing SchemaVersion in manually instantiated NetMessages. Mitigation: Update all test NetMessage factories/constructors.
   - Client disconnection due to missing SchemaVersion. Mitigation: Centralize auto-injection in NetworkClient.SendNetMessage.
8) NEXT: CORE-002: Sessions + sequence/nonce per client for anti-replay protection.
