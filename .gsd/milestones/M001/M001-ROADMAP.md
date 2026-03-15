# M001: Core Multiplayer Systems

**Vision:** Deliver production-ready multiplayer systems for TWL's commercial launch by completing persistence, security, social systems, progression, economy, and content foundations around the existing server-authoritative MMORPG core.

## Success Criteria

- Core multiplayer loop works end-to-end across party, guild, market, and rebirth systems.
- Persistence and security foundations are strong enough for valuable multiplayer operations.
- Content and progression blockers are resolved so players can advance without broken quest chains or data integrity failures.

## Slices

- [x] **S01: Infrastructure Foundation** `risk:medium` `depends:[]`
  > After this: Set up EF Core 10 infrastructure with GameDbContext, entity configurations, NpgsqlDataSource connection pooling, and initial database migration.
- [x] **S02: Security Hardening** `risk:medium` `depends:[S01]`
  > After this: Implement packet replay protection using nonce + timestamp validation with a strict 30-second window.
- [x] **S03: Content Quality** `risk:medium` `depends:[S02]`
  > After this: Stabilize Hidden Ruins and Ruins Expansion content so quest chains are internally consistent and regression-safe.
- [x] **S04: Party System** `risk:medium` `depends:[S03]`
  > After this: Implement the server-authoritative party lifecycle foundation (create/invite/accept/decline/leave/kick) and align quest gating with party membership rules.
- [x] **S05: Guild System** `risk:medium` `depends:[S04]`
  > After this: Implement the guild lifecycle foundation: guild creation, membership invite flow, leave/kick flow, and core network contracts.
- [x] **S06: Rebirth System** `risk:medium` `depends:[S05]`
  > After this: Implement character rebirth transactional foundation, including formula policy, atomic state mutation, persistence history, and network entry points.
- [x] **S07: P2P Market System** `risk:medium` `depends:[S06]`
  > After this: Create Phase 7 market foundation: domain service, persistence schema, and network contracts.
- [x] **S08: Compound System** `risk:medium` `depends:[S07]`
  > After this: Create Phase 8 compound foundation contracts and persistence metadata.
- [x] **S09: Pet System Completion** `risk:medium` `depends:[S08]`
  > After this: Harden pet combat AI to satisfy PET-01 with deterministic, intelligent action selection.
- [ ] **S10: Combat & Progression Integration** `risk:medium` `depends:[S09]`
  > After this: Implement death-penalty EXP loss on player death (`CMB-01` partial) using server-authoritative combat event handling.
- [ ] **S11: Content Foundation** `risk:medium` `depends:[S10]`
  > After this: Expand `items.json` with the remaining higher-tier item sets and content coverage needed for the full item database.
- [ ] **S12: Quest Expansion** `risk:medium` `depends:[S11]`
  > After this: Create the main story quest chains for regions 3 and 4 (Selva Esmeralda and Arrecife Hundido), covering levels 20-45.
- [ ] **S13: World Expansion** `risk:medium` `depends:[S12]`
  > After this: Finalize spawn tables for Regions 1 and 2 (Isla Brisa and Puerto Roca) using expanded monster data.
