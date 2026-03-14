# The Wonderland Legacy (TWL)

## What This Is

The Wonderland Legacy is a 2D turn-based MMORPG inspired by Wonderland Online, set in **Las Islas Perdidas** - a tropical archipelago where shipwreck survivors explore ancient ruins, build a community, and uncover the secrets of a lost civilization called **Los Ancestrales** who mastered elemental energy through crystalline technology (**Resonancia**). Players experience tactical party-based combat, deep character progression through rebirth, pet companions, and a player-driven economy in a persistent shared world.

Currently at **Proto-Alpha** stage with foundational architecture complete (server-authoritative, 3-layer separation, basic networking and combat). This milestone focuses on completing core multiplayer systems to reach production readiness for commercial launch.

## Core Value

The multiplayer gameplay loop works end-to-end: players can party up for tactical combat, trade items through a player-driven market, form guilds with shared resources, progress their characters through rebirth mechanics, and experience deep pet companion gameplay - all in a secure, persistent shared world.

## Requirements

### Validated

<!-- Shipped and confirmed valuable -->

- ✓ Server-authoritative architecture with clean 3-layer separation (Shared/Client/Server) — existing
- ✓ Network protocol and basic multiplayer connectivity — existing
- ✓ Turn-based combat system with elemental cycle (Earth/Water/Fire/Wind) — existing
- ✓ Quest system foundation with JSON-driven content — existing (needs fixes)
- ✓ Pet system foundation (capture, leveling, stats) — existing (incomplete)
- ✓ Premium economy (Gems/Shop with secure idempotent transactions) — existing
- ✓ Inventory and equipment system with stat bonuses — existing
- ✓ Data-driven content pipeline (JSON-based skills, quests, pets, items) — existing
- ✓ Visual customization (palette swapping + equipment layering) — existing
- ✓ Tiled map rendering and world navigation — existing
- ✓ **PostgreSQL Migration (S01)** - Replace FilePlayerRepository (JSON) with PostgreSQL for production-grade persistence
- ✓ **Security Hardening (S02)** - Authoritative movement validation and packet replay protection
- ✓ **Content Quality (S03)** - Fixed 8 failing tests (quest chains, localization keys)
- ✓ **Party System (S04)** - Players can invite/join/leave parties (max 4), share XP and loot distribution, manage party formation
- ✓ **Guild System (S05)** - Players can create/join/leave guilds, use guild chat, deposit/withdraw from guild storage
- ✓ **Rebirth System (S06)** - Characters can rebirth to gain permanent stat bonuses and reset level for enhanced progression
- ✓ **P2P Market System (S07)** - Players can list items for sale, browse listings, buy from other players, with centralized ledger and automatic tax/gold transfer

### Active

<!-- Current scope. Building toward these -->

- [ ] **Compound System (S08)** - Players can enhance equipment through the compound system
- [ ] **Pet Combat AI (S09)** - Pets make intelligent skill selections in combat (not random)
- [ ] **Pet Data Population (S09)** - pets.json populated with complete starter region pet roster
- [ ] **Pet Amity & Bonding (S09)** - Amity loss on KO, bonding mechanics, riding system
- [ ] **Death Penalty (S10)** - 1% EXP loss and 1 durability loss per equipped item on death
- [ ] **Instance Lockouts (S10)** - 5 dungeon runs per day per character with daily reset
- [ ] **Full Combat Flow (S10)** - Complete combat loop with all mechanics (death penalty, pet AI, skill effects) integrated
- [ ] **Content Expansion (S11-S13)** - 200+ items, 80+ monsters, quest chains for all 8 regions, spawn tables

### Out of Scope

<!-- Explicit boundaries -->

- New map regions (Selva Esmeralda, Isla Volcana, Arrecife Hundido) — post-launch expansion (beyond core foundational work)
- Housing system (tent, furniture grid, manufacturing) — separate milestone
- PvP systems (duels, arenas, territory control) — post-launch feature
- Advanced endgame content (legendary dungeons, raid bosses) — post-launch content
- Mobile platform support — PC-first, mobile later

## Context

**Commercial Launch Target**: This project is building toward a commercial release of a turn-based MMORPG that fills the niche left by classic games like Wonderland Online.

**Current State**: Proto-Alpha with vertical slice complete. Clean architecture established (server-authoritative, data-driven, 3-layer separation). Basic multiplayer, combat, and content systems functional but incomplete. Production gaps identified in persistence, security, and core multiplayer systems.

**Wonderland Online Inspiration**: Core mechanics preserved from WLO - turn-based combat, pet companion system, rebirth progression, quest-driven gameplay, crafting systems. Modernized with updated graphics/UI, quality-of-life features, and enhanced social systems.

**Technical Foundation**:
- **Stack**: C#/.NET, MonoGame (DesktopGL), PostgreSQL
- **Architecture**: Server-authoritative with TWL.Shared (domain), TWL.Client (MonoGame presentation), TWL.Server (authoritative state)
- **Content Pipeline**: JSON-driven (skills, quests, pets, items, equipment, monsters)
- **World**: Las Islas Perdidas archipelago with map ID ranges (Isla Brisa: 0001-0099, Puerto Roca: 1000-1099, etc.)

**Production Risks Identified**:
- (Resolved) Persistence using JSON files instead of PostgreSQL
- (Resolved) Missing P2P economy prevents player-driven market gameplay
- (Resolved) Weak security (movement validation, replay protection)
- (Resolved) Broken quest chains and missing localization
- (Resolved) Party/guild systems foundations

## Constraints

- **Architecture**: Must maintain server-authoritative model — prevents cheating, enables secure MMO gameplay
- **Persistence**: Must use PostgreSQL for production — ACID transactions, handles concurrency
- **Security**: Must pass security audit (movement validation, anti-replay, rate limiting)
- **Testing**: All content validation tests must pass
- **Platform**: PC-first (MonoGame/DesktopGL)
- **Timeline**: Commercial launch target — quality and production-readiness are non-negotiable

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Server-authoritative architecture | Prevents cheating, enables secure MMO gameplay, allows authoritative combat resolution | ✓ Good - Core to game integrity |
| 3-layer separation (Shared/Client/Server) | Clean architecture, testable domain logic, supports future mobile client | ✓ Good - Maintainable and extensible |
| Data-driven content (JSON) | Enables rapid content iteration without code changes, supports modding potential | ✓ Good - Fast content development |
| MonoGame/DesktopGL for client | Cross-platform potential, full control over rendering, active community | — Pending - Will validate with PC launch |
| PostgreSQL for persistence | Production-grade ACID transactions, handles concurrency | ✓ Validated - Migration complete |
| Turn-based combat (not action) | Preserves WLO identity, enables tactical depth, reduces server tick rate requirements | ✓ Good - Aligns with target audience |
| Elemental cycle system | Creates strategic depth, balances PvE/PvP, differentiates from generic fantasy | ✓ Good - Core gameplay identity |
| Semi-Atomic Batch Trade | Ensures consistency for multi-item transfers without distributed transactions | ✓ Validated - S07 completion |

---
*Last updated: 2026-03-14 after S07 completion*
