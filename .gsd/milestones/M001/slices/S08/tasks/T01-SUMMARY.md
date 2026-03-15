---
milestone: M001
slice: S08
task: T01
status: completed
---

# T01: 08-compound-system 01 — Summary

Created Phase 8 compound foundation contracts and persistence metadata.

## Key Changes

### Shared Domain
- **DTOs**: Added `CompoundDTOs.cs` defining `CompoundRequestDTO`, `CompoundResponseDTO`, and `CompoundOutcome` enum.
- **Models**: Updated `Item.cs` to include `InstanceId` (Guid), `EnhancementLevel` (int), and `EnhancementStats` (Dictionary<string, float>).

### Server Simulation
- **Service**: Added `ICompoundService.cs` interface.
- **Manager**: Implemented `CompoundManager.cs` as a server-authoritative service stub with logging for operation start/end.
- **Persistence**: Updated `ServerCharacter.cs` to ensure `InstanceId`, `EnhancementLevel`, and `EnhancementStats` are correctly copied during projections (Inventory/Equipment/Bank) and persisted in `GetSaveData`/`LoadSaveData`.
- **Hardening**: Updated `ServerCharacter.AddItem` to prevent stacking of enhanced items with non-enhanced items by checking `EnhancementLevel == 0` during stack search.
- **DI**: Registered `ICompoundService` as a singleton in `Program.cs`.

### Verification
- **Tests**: Created `TWL.Tests/Compound/CompoundContractTests.cs` covering:
  - Item enhancement metadata persistence.
  - `ServerCharacter` save/load roundtrip for enhancement metadata.
  - `CompoundManager` stub behavior.

## Must-Haves Verification
- [x] "Compound operations execute through a dedicated server-authoritative service": Verified by `ICompoundService`/`CompoundManager` implementation.
- [x] "Equipment enhancement metadata persists across save/load": Verified by `ServerCharacter_ShouldRoundtripEnhancementMetadata` test.
- [x] "Compound service is registered in runtime DI": Verified in `Program.cs`.

## Observations
- Added `InstanceId` (Guid) to `Item` to allow precise identification of items in the inventory, which is critical for compounding and other unique item operations.
- Updated all internal `Item` copy projections in `ServerCharacter` to include the new metadata fields.
- Hardened `AddItem` to ensure only non-enhanced items stack by default.

## Next Steps
- T02 will implement the networking opcodes and session handlers to expose the compound service to the client via NPC interactions.
