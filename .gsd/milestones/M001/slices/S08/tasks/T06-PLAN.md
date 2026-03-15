# Task T06: Finalize compound client integration

## Goal
Finalize the client-side integration of the compound system, ensuring it correctly interacts with the server-authoritative logic implemented in previous tasks. Complete Phase 8 with full verification coverage.

## Must-Haves
- [x] Resolve merge conflicts and build errors in the repository.
- [ ] Implement client-side opcodes and handlers for `SMSG_COMPOUND_REQUEST_START_ACK` and `CompoundResponse`.
- [ ] Implement client-side `CompoundRequest` sending logic in `GameClientManager` or `ClientInventoryManager`.
- [ ] Ensure `ForgeSystem` logic is either migrated to use server requests or clearly separated as a legacy/offline system.
- [ ] Create integration tests verifying the client-to-server compound flow.
- [ ] Verify all slice-level verification checks.

## Steps
1. **Infrastructure**: Add `Compound` opcodes to `NetworkClient.HandleServerMessage`.
2. **State Management**: Add events and handlers for compound operations in `GameClientManager`.
3. **Integration**: Connect `ForgeSystem` or a new `CompoundUI` logic to send `CompoundRequest` via `NetworkClient`.
4. **Testing**:
    - Create `TWL.Tests/Compound/CompoundClientIntegrationTests.cs` to mock the server and verify client behavior.
    - Create `TWL.Tests/Compound/CompoundPhaseAcceptanceTests.cs` for full slice validation.
5. **Verification**: Run `pwsh -File scripts/verify.ps1` and ensure all tests pass.

## Observability Impact
- Client logs should show when a compound window is requested by the server.
- Client logs should show the outcome of compound operations received from the server.
