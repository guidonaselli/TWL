# Requirements

## Validated

### INFRA-01 — PostgreSQL persistence replaces FilePlayerRepository with atomic transactions and connection pooling
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S01

PostgreSQL persistence replaces FilePlayerRepository with atomic transactions and connection pooling

### INFRA-02 — Entity Framework Core 10.0 handles complex write operations (market, guild bank) with migrations
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S01

Entity Framework Core 10.0 handles complex write operations (market, guild bank) with migrations

### INFRA-03 — Dapper handles high-performance read operations (market browsing, guild rosters)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S01

Dapper handles high-performance read operations (market browsing, guild rosters)

### SEC-01 — Movement validation prevents speed-hacks and teleportation (server calculates max distance per tick)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S02

Movement validation prevents speed-hacks and teleportation (server calculates max distance per tick)

### SEC-02 — Packet replay protection using nonce + timestamp validation (30-second validity window)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S02

Packet replay protection using nonce + timestamp validation (30-second validity window)

### SEC-03 — Market transactions use Serializable isolation level to prevent race condition duplication
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S02

Market transactions use Serializable isolation level to prevent race condition duplication

### SEC-04 — All multi-party operations have idempotency keys extending EconomyManager pattern
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S02

All multi-party operations have idempotency keys extending EconomyManager pattern

### PTY-01 — Player can create party (max 4 members) and invite other players
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S04

Player can create party (max 4 members) and invite other players

### PTY-02 — Player can accept/decline party invitations
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S04

Player can accept/decline party invitations

### PTY-03 — Player can leave party or leader can kick members (not during combat)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S04

Player can leave party or leader can kick members (not during combat)

### PTY-04 — Party members share XP with proximity checks (must be on same map, within range)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S04

Party members share XP with proximity checks (must be on same map, within range)

### PTY-05 — Party members share loot distribution using round-robin or need/greed roll system
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S04

Party members share loot distribution using round-robin or need/greed roll system

### PTY-08 — Tactical formation system allows 3x3 grid positioning (front/mid/back rows)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S04

Tactical formation system allows 3x3 grid positioning (front/mid/back rows)

### PTY-09 — Party kick is disabled during combat and boss fights to prevent kick abuse
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S04

Party kick is disabled during combat and boss fights to prevent kick abuse

### GLD-01 — Player can create guild with unique name and configurable creation fee (gold sink)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S05

Player can create guild with unique name and configurable creation fee (gold sink)

### GLD-02 — Player can invite other players to guild with acceptance flow
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S05

Player can invite other players to guild with acceptance flow

### GLD-03 — Player can leave guild or be kicked by authorized members
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S05

Player can leave guild or be kicked by authorized members

### GLD-04 — Guild has hierarchical rank system with granular permissions (invite, promote, kick, withdraw storage)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S05

Guild has hierarchical rank system with granular permissions (invite, promote, kick, withdraw storage)

### GLD-05 — Guild chat channel broadcasts to all guild members (persists when offline)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S05

Guild chat channel broadcasts to all guild members (persists when offline)

### GLD-06 — Guild shared storage allows deposit/withdraw with permission-based access control
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S05

Guild shared storage allows deposit/withdraw with permission-based access control

### GLD-07 — Guild bank withdrawal operations have audit logs tracking who/what/when
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S05

Guild bank withdrawal operations have audit logs tracking who/what/when

### GLD-08 — New guild members have 1-2 week time gate before storage withdrawal access
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S05

New guild members have 1-2 week time gate before storage withdrawal access

### GLD-09 — Guild roster displays member list with online status, last login, rank
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S05

Guild roster displays member list with online status, last login, rank

### MKT-01 — Player can create item listing with price, quantity, and expiration (24-72 hours)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S07

Player can create item listing with price, quantity, and expiration (24-72 hours)

### MKT-02 — Player can search market listings with filters (item name, type, price range, rarity)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S07

Player can search market listings with filters (item name, type, price range, rarity)

### MKT-04 — Player can cancel own listing before purchase (item returned to inventory)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S07

Player can cancel own listing before purchase (item returned to inventory)

### MKT-05 — Market displays price history showing min/avg/max prices for last N transactions per item
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S07

Market displays price history showing min/avg/max prices for last N transactions per item

### MKT-03 — Player can purchase listing with atomic gold/item transfer and automatic tax deduction
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S07

Player can purchase listing with atomic gold/item transfer and automatic tax deduction

### MKT-06 — Transaction fees (5-10% configurable) are deducted from seller proceeds as economy sink
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S07

Transaction fees (5-10% configurable) are deducted from seller proceeds as economy sink

### MKT-07 — Listings expire after configured duration and items return to seller inventory
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S07

Listings expire after configured duration and items return to seller inventory

### MKT-08 — Direct player-to-player trade window for face-to-face trading with both-party confirmation
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S07

Direct player-to-player trade window for face-to-face trading with both-party confirmation

### REB-01 — Character can rebirth at level 100+ resetting to level 1 with permanent stat bonuses (10-20 points)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S06

Character can rebirth at level 100+ resetting to level 1 with permanent stat bonuses (10-20 points)

### REB-02 — Character rebirth requirements include minimum level and optional quest/item requirement
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S06

Character rebirth requirements include minimum level and optional quest/item requirement

### REB-03 — Character rebirth count is tracked and displayed in character info/nameplate (visible prestige)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S06

Character rebirth count is tracked and displayed in character info/nameplate (visible prestige)

### REB-04 — Character retains skill trees and equipment after rebirth (can use all gear at level 1)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S06

Character retains skill trees and equipment after rebirth (can use all gear at level 1)

### REB-05 — Rebirth operation is atomic transaction (all-or-nothing stat changes with rollback safety)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S06

Rebirth operation is atomic transaction (all-or-nothing stat changes with rollback safety)

### REB-06 — Rebirth history has audit trail for debugging and rollback capability
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S06

Rebirth history has audit trail for debugging and rollback capability

### REB-07 — Diminishing returns formula applies (Rebirth 1: 20 stats, 2: 15, 3: 10, 4: 5) to prevent infinite scaling
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S06

Diminishing returns formula applies (Rebirth 1: 20 stats, 2: 15, 3: 10, 4: 5) to prevent infinite scaling

### PET-03 — Quest pets can rebirth/evolve while capturable pets cannot (preserved differentiation from WLO)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S06

Quest pets can rebirth/evolve while capturable pets cannot (preserved differentiation from WLO)

### PET-04 — Pet rebirth grants stat bonuses and evolution to new forms (Pet Rebirth 1: 10 stats, 2: 8, 3: 5)
- Status: validated
- Class: core-capability
- Source: inferred
- Primary Slice: S06

Pet rebirth grants stat bonuses and evolution to new forms (Pet Rebirth 1: 10 stats, 2: 8, 3: 5)

## Active

### CONT-01 — `items.json` contains 200+ items across 8 tiers (Lv1-100) covering consumables, crafting materials, quest items
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S11

`items.json` contains 200+ items across 8 tiers (Lv1-100) covering consumables, crafting materials, quest items


### PET-05 — Pet amity decreases by 1 on KO (knockout) in combat
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S09

Pet amity decreases by 1 on KO (knockout) in combat

### PET-06 — Pet bonding mechanic rewards high amity with stat bonuses or special abilities
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S09

Pet bonding mechanic rewards high amity with stat bonuses or special abilities

### PET-07 — Pet riding system allows player to mount pets for movement speed bonus
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S09

Pet riding system allows player to mount pets for movement speed bonus

### CMB-01 — Death penalty deducts 1% of current level EXP (floor 0%) on character death
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S10

Death penalty deducts 1% of current level EXP (floor 0%) on character death

### CMB-02 — Death penalty deducts 1 durability from all equipped items on character death
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S10

Death penalty deducts 1 durability from all equipped items on character death

### CMB-03 — Equipment with 0 durability enters "Broken" state (stats disabled until repaired)
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S10

Equipment with 0 durability enters "Broken" state (stats disabled until repaired)

### CMB-04 — Full combat flow integrates death penalty, pet AI, skill effects, and status system
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S10

Full combat flow integrates death penalty, pet AI, skill effects, and status system

### INST-01 — Instance system tracks dungeon runs per character per day (max 5)
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S10

Instance system tracks dungeon runs per character per day (max 5)

### INST-02 — Instance lockout counter resets daily at server midnight (00:00 UTC)
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S10

Instance lockout counter resets daily at server midnight (00:00 UTC)

### INST-03 — Instance entry is rejected if player has reached daily limit (5/5 runs)
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S10

Instance entry is rejected if player has reached daily limit (5/5 runs)

### CONT-02 — `monsters.json` contains 80+ monsters across 10+ families with Earth/Water/Fire/Wind variants per family
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S11

`monsters.json` contains 80+ monsters across 10+ families with Earth/Water/Fire/Wind variants per family

### CONT-03 — `pets.json` contains 50+ pets with skills, evolution paths, capture rules, and utility roles
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S11

`pets.json` contains 50+ pets with skills, evolution paths, capture rules, and utility roles

### CONT-04 — Quest chains exist for all 8 regions with proper Requirements linking (no dead-ends)
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S12

Quest chains exist for all 8 regions with proper Requirements linking (no dead-ends)

### CONT-05 — Side quest arcs cover crafting, pet capture, exploration, and special skill trials
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S12

Side quest arcs cover crafting, pet capture, exploration, and special skill trials

### CONT-06 — Spawn tables exist for all regions with 3-5 mobs per map and boss spawns in dungeons
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S13

Spawn tables exist for all regions with 3-5 mobs per map and boss spawns in dungeons

### CONT-07 — Map region directories and metadata exist for regions 3-8 (Selva Esmeralda through Resonancia Core)
- Status: active
- Class: core-capability
- Source: inferred
- Primary Slice: S13

Map region directories and metadata exist for regions 3-8 (Selva Esmeralda through Resonancia Core)

## Deferred

## Out of Scope

## Discovered Requirements

### [DISCOVERED] PET-08 — Player needs a way to revive KO'd pets (consumable item or NPC healer)
- Status: discovered
- Class: core-capability
- Source: inferred
- Primary Slice: S09

Player needs a way to revive KO'd pets (consumable item or NPC healer)

### [DISCOVERED] WORLD-01 — Map transition portal system to move between different regions (e.g. Isla Brisa to Puerto Roca)
- Status: discovered
- Class: core-capability
- Source: architect
- Primary Slice: S13

Map transition portal system is required to seamlessly move between different regions and dungeon instances.
