# T05: 07-p2p-market-system 05

**Slice:** S07 — **Milestone:** M001

## Description

Implement direct player-to-player trade window and finalize client market/trade integration.

Purpose: This delivers MKT-08 and completes the market phase with a server-authoritative live trading path.
Output: Trade session manager + trade DTO/opcode routing + client trade window integration + direct-trade regression tests.

## Must-Haves

- [ ] "Players can run direct player-to-player trade with both-party confirmation before transfer"
- [ ] "Direct trade honors existing bind-policy transfer restrictions and rejects unsafe transfers"
- [ ] "Client market/trade UI reflects server-authoritative trade state changes"

## Files

- `TWL.Server/Simulation/Managers/TradeSessionManager.cs`
- `TWL.Server/Simulation/Managers/TradeManager.cs`
- `TWL.Server/Simulation/Networking/ClientSession.cs`
- `TWL.Shared/Domain/DTO/TradeDTOs.cs`
- `TWL.Shared/Net/Network/Opcode.cs`
- `TWL.Client/Presentation/Managers/GameClientManager.cs`
- `TWL.Client/Presentation/Managers/ClientMarketplaceManager.cs`
- `TWL.Client/Presentation/UI/UiTradeWindow.cs`
- `TWL.Tests/Market/DirectTradeWindowTests.cs`
- `TWL.Tests/Market/MarketTradeIntegrationTests.cs`
