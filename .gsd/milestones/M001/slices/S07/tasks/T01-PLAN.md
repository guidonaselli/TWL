# T01: 07-p2p-market-system 01

**Slice:** S07 — **Milestone:** M001

## Description

Create Phase 7 market foundation: domain service, persistence schema, and network contracts.

Purpose: This establishes the server-authoritative base needed for all listing/search/purchase/trade features.
Output: Market service interface + manager, DTO/opcode surface, persistence schema updates, and contract tests.

## Must-Haves

- [ ] "Players can interact with a server-authoritative market API rather than local in-memory listings"
- [ ] "Market listings have explicit persisted state and lifecycle identity"
- [ ] "Market operations are reachable through dedicated network contracts"

## Files

- `TWL.Server/Simulation/Managers/IMarketService.cs`
- `TWL.Server/Simulation/Managers/MarketManager.cs`
- `TWL.Server/Persistence/Database/DbService.cs`
- `TWL.Shared/Domain/DTO/MarketDTOs.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Server/Simulation/Program.cs`
- `TWL.Tests/Market/MarketContractTests.cs`
