# T04: 09-pet-system-completion 04 - Summary

Implemented the end-to-end riding system flow for PET-07, connecting pet utility requests to movement speed bonuses and client-side state synchronization.

## Key Changes

### Server-Side
- **PetService**: Updated `UseUtility` to correctly toggle `IsMounted` and `MoveSpeedModifier` on the character. It now also sends a `StatsUpdate` packet to the client upon change.
- **ClientSession**: Added routing for `PetActionType.Utility` in `HandlePetActionAsync`, allowing players to trigger mounting via pet action requests.
- **MovementValidator**: Modified the `Validate` method to take the character's `moveSpeedModifier` into account when calculating maximum allowed axis delta and Euclidean distance, preventing speed-hack false positives for mounted players.

### Shared Domain
- **Character**: Added `IsMounted` and `MoveSpeedModifier` properties. Refactored `MovementSpeed` to be a derived property (`BaseMovementSpeed * MoveSpeedModifier`) for consistent speed calculation across client and server logic.

### Client-Side
- **GameClientManager**: Added `HandleStatsUpdate` to process server-sent state updates and exposed the `OnStatsUpdated` event.
- **NetworkClient**: Integrated `Opcode.StatsUpdate` handling to dispatch updates to the `GameClientManager`.
- **SceneGameplay**: Subscribed to `OnStatsUpdated` to reflect mounting state and speed modifiers on the local `PlayerCharacter` instance.

## Verification Results

### Automated Tests
- `PetRidingSystemTests`: Verified that using the mount utility toggles the character's mount state and correctly applies/removes the speed bonus.
- `PetActionUtilityHandlerTests`: Verified that pet action requests for utilities are correctly routed from the network layer to the `PetService`.
- `MovementValidatorTests`: Updated and added new tests to ensure that the server-side movement validation correctly scales limits based on the character's speed modifier.
- `scripts/verify.ps1`: Full suite passed (ignoring a transient flake in `WorldSchedulerTests` which passed on retry).

### Manual Verification Signals
- Server logs now show when a player toggles mounting state.
- `Character.MovementSpeed` automatically reflects the bonus from `MoveSpeedModifier`.

## Traceability
- Must-Have: "Players can trigger riding utility through pet action request flow" -> **DONE**
- Must-Have: "Mount state applies an actual movement speed bonus during gameplay movement updates" -> **DONE**
- Must-Have: "Client receives and reflects mount/riding state from server-authoritative responses" -> **DONE**
