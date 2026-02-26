1) TITLE: Implement Strict Protocol Versioning (CORE-001)
2) TYPE: PR
3) SCOPE (IN):
   - TWL.Shared/Net/Network/NetMessage.cs
   - TWL.Server/Simulation/Networking/ClientSession.cs
   - TWL.Client/Presentation/Networking/NetworkClient.cs
   - TWL.Tests/Security/ClientSessionSchemaVersionTests.cs
4) OUT-OF-SCOPE:
   - Persistence implementation or migration
   - Client UI or Gameplay logic
   - Other security features (RateLimit/ReplayGuard modification beyond integration)
5) ACCEPTANCE CRITERIA (DoD):
   - [ ] `NetMessage` DTO includes `public int? SchemaVersion { get; set; }`.
   - [ ] `ClientSession.HandleMessageAsync` validates `msg.SchemaVersion` matches `ProtocolConstants.CurrentSchemaVersion` (Fail-Closed).
   - [ ] Validation occurs strictly **after** `ReplayGuard` and `RateLimiter`, but **before** `Opcode` dispatch.
   - [ ] Mismatch or null version triggers `Metrics.RecordValidationError`, `SecurityLogger` event, and immediate `Disconnect`.
   - [ ] `NetworkClient.SendNetMessage` automatically populates `SchemaVersion` from `ProtocolConstants`.
6) REQUIRED TESTS / VALIDATIONS:
   - Create `TWL.Tests/Security/ClientSessionSchemaVersionTests.cs` covering:
     - `HandleMessageAsync_WithValidSchema_Proceeds`
     - `HandleMessageAsync_WithInvalidSchema_Disconnects`
     - `HandleMessageAsync_WithMissingSchema_Disconnects`
   - Verify regression: `ClientSessionReplayProtectionTests` must pass (may require test setup updates to include SchemaVersion).
7) RISKS:
   - **Breaking Protocol**: Legacy clients without `SchemaVersion` will be disconnected. Mitigation: Deploy Client and Server updates simultaneously.
   - **Test Breakage**: Existing security tests using `NetMessage` will fail validation. Mitigation: Update `TestableClientSession` or test data to include `CurrentSchemaVersion`.
8) NEXT: CORE-002: Session Sequence/Nonce Hardening.
