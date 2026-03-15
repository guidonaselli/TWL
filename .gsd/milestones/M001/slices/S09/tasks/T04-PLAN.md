# T04: 09-pet-system-completion 04

**Slice:** S09 — **Milestone:** M001

## Description

Implement end-to-end riding system flow for PET-07.

Purpose: Close the current utility/riding gap by connecting request handling, mount-state effects, and client-visible movement behavior.
Output: Utility action routing, riding movement integration, and riding behavior regression tests.

## Must-Haves

- [ ] "Players can trigger riding utility through pet action request flow"
- [ ] "Mount state applies an actual movement speed bonus during gameplay movement updates"
- [ ] "Client receives and reflects mount/riding state from server-authoritative responses"

## Files

- `TWL.Shared/Domain/Requests/PetActionRequest.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Services/PetService.cs`
- `TWL.Server/Simulation/Networking/ServerCharacter.cs`
- `TWL.Client/Presentation/Managers/GameClientManager.cs`
- `TWL.Client/Presentation/Scenes/SceneGameplay.cs`
- `TWL.Tests/PetTests/PetRidingSystemTests.cs`
- `TWL.Tests/PetTests/PetActionUtilityHandlerTests.cs`

## Steps

1. **Test Foundation**: Create `PetRidingSystemTests.cs` and `PetActionUtilityHandlerTests.cs` to define expected behavior for utility action requests and movement speed bonuses.
2. **Request Routing**: Update `PetService` to handle `PetActionRequest` with `PetActionType.Ride`.
3. **Server State**: Modify `ServerCharacter` to track `IsRiding` state and calculate `MovementSpeed` dynamically based on mount status.
4. **Networking**: Ensure `ClientSession` synchronizes the `IsRiding` state to the client via state updates or dedicated response packets.
5. **Client Presentation**: Update `GameClientManager` and `SceneGameplay` to visually reflect the riding state (e.g., character sprite changes or speed adjustment).
6. **Verification**: Run `scripts\verify.ps1` and ensure all pet-related tests pass.

## Observability Impact

- **Logs**: `PetService` will log when a player starts or stops riding a pet.
- **Server Character State**: `ServerCharacter.MovementSpeed` will be inspectable in tests and debug logs to verify the bonus is applied.
- **Client Logs**: The client will log receipt of the riding state update.
- **Test Results**: `PetRidingSystemTests` will provide deterministic verification of speed bonuses and state transitions.
