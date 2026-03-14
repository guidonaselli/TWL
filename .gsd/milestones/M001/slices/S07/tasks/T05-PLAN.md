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

## Steps

1. **Domain & Network**: Define `TradeDTOs` and add trade `Opcodes`.
2. **Server Infrastructure**: Implement `TradeSessionManager` to manage active 1-on-1 sessions and state (pending, locked, confirmed).
3. **Server Logic**: Update `TradeManager` to handle trade invitations and coordinate with `TradeSessionManager`.
4. **Networking**: Wire up trade opcodes in `ClientSession`.
5. **Client Presentation**: Implement `UiTradeWindow` and update `ClientMarketplaceManager`/`GameClientManager` to handle trade state sync.
6. **Tests**: Implement `DirectTradeWindowTests` and `MarketTradeIntegrationTests`.
7. **Verification**: Run all S07 tests to ensure no regressions in the market system.

## Observability Impact

- **Trade Logs**: `TWL.Server` will log trade start, item/gold offering updates, locking, and final commitment/cancellation.
- **Session State**: `TradeSessionManager` will track session duration and party status, visible in server diagnostics.
- **Failure Visibility**: Trade rejections (due to bind policy or inventory space) will be logged with specific reasons and sent as DTO errors to clients.
