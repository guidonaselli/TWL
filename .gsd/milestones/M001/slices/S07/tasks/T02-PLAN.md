# T02: 07-p2p-market-system 02

**Slice:** S07 — **Milestone:** M001

## Description

Implement listing lifecycle operations including create, cancel, and automatic expiration return.

Purpose: This delivers MKT-01, MKT-04, and MKT-07 with abuse-resistant lifecycle handling.
Output: Listing state machine + expiration scheduler + lifecycle regression tests.

## Must-Haves

- [ ] "Players can create listings with price, quantity, and expiration window"
- [ ] "Players can cancel their own active listings and receive item return"
- [ ] "Expired listings return unsold items automatically to seller inventory"

## Files

- `TWL.Server/Simulation/Managers/MarketManager.cs`
- `TWL.Server/Simulation/Managers/MarketListingScheduler.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Persistence/Services/PlayerService.cs`
- `TWL.Shared/Domain/DTO/MarketDTOs.cs`
- `TWL.Tests/Market/MarketListingLifecycleTests.cs`
- `TWL.Tests/Market/MarketExpirationTests.cs`
