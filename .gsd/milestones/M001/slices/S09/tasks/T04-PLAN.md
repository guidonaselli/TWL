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
