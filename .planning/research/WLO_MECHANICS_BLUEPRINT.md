# Architectural Blueprint and Systems Mechanics Documentation for a Wonderland Online Mobile Framework

## Source Extracted: 2026-03-06

This document contains explicit mechanical constraints and mathematical logic extracted from deep research documentation regarding Wonderland Online. It serves as a single source of truth for implementing accurate mechanics in the MonoGame/C# architecture.

### 1. Core Combat & State Machine
- **Determinism**: The combat engine is a 2-phase turn-based loop (Command Phase, Action Phase).
- **Priority Queue**: Action Phase execution order is strictly determined by descending Speed attribute at the exact start of the phase.
- **Overkill Rule**: In a single hit, damage exceeding a specific max HP threshold immediately forcibly respawns players or removes pets from the instance without further turn participation.

### 2. Elemental Matrix
- **Types**: Fire, Water, Earth, Wind.
- **Cycle**: Fire > Wind > Earth > Water > Fire.
- **Logic**: Element determines base stat multipliers, available skill trees, and combat multipliers. Elemental buffs stack multiplicatively. Damage calculation happens in Action Phase using a 4x4 resolution matrix.

### 3. "Bursting" (Exponential XP) & Automation Edge Cases
- **Mechanic**: Achieving massive overkill damage to yield exponential XP that bypasses linear grinding constraints.
- **Trigger Condition**: Target must have an active DEF debuff state (e.g., specific skills that truncate mitigation attributes).
- **Execution**: Three high-damage element characters (e.g., Fire) deal synchronized max damage in the same Action Phase queue. Overkill damage is mathematically pooled into an accumulator. 
- **Party Composition Math**: Uses four Level 1 pets to keep the `PartyTotalLevel` average artificially low while maximizing the `PartyBonusMultiplier` (8 actors present). These pets *must* have lower Speed than players so they act last and don't break the overkill queue sequence.
- **Party Disband / Automation Fallback**: If the party leader disconnects, the server must automatically execute a "party disbandment protocol". Additionally, if a client's automated combat routine (bot) exhausts resource reserves (SP), it forces a basic physical attack fallback, which inherently breaks the Action Phase overkill chain.

### 4. Entity Bifurcation (Pets vs Mobs)
- **Creature Pets (Mobs)**: Can be captured. Can have the `MountableComponent`. When equipped with Saddles (e.g., Swift Saddle), they pass a precise percentage (e.g., 33.3%) of their specific attribute to the rider. Cannot participate in battle while mounted (mutual exclusivity).
- **Human Pets**: Acquired via narrative quests. Cannot be mounted. Superior stats. Exclusive access to the Rebirth progression system.

### 5. Pet Stats & Amity Penalties
- **Level Up RNG**: Pet stat points are randomly clustered—a single point goes to a random index among the pet's *top three highest existing attributes*.
- **Washing**: Players must use "Lethe Scrolls" (only usable if pet >= Level 20) to manually decrement and reallocate bad stat rolls.
- **Amity Penalty**: Desertion occurs instantly if Amity <= 20, permanently deleting the pet AND all equipped items (including premium cash shop gear) from the database. Trade causes an immediate -10 Amity. "Friendship Brooch" halts Amity drain, but operates *only* if Amity is exactly 100.

### 6. Progression Systems (Evolution vs Rebirth)
- **Creature Evolution**: Uses tier-specific Evolution Stones (mapped to the 4 base elements) + requires all skill points allocated on the pet. 100% success rate to advance to next stage (Base -> Stage 2 -> Stage 3 -> Stage 4).
  - *Future Consideration*: The system can theoretically support non-standard elements (e.g., Light/Dark or Sun/Moon) in future expansions, but the core V1 relies strictly on the 4 base elements.
- **Human Pet Rebirth**: Gated by a high level requirement (Lv 100). Multi-step narrative sequence: 
  - 1. A mandatory "Death Quest" where the companion is mechanically killed and removed from the active party.
  - 2. A restoration phase (trading premium or rare currency to an NPC) to restore the physical body.
  - 3. A final soul-merging phase in a hidden location.
  - *Reward*: Unlocks a highly powerful, character-specific "Signature Skill" and significantly boosts late-game stat scaling.
- **Character Rebirth**: High-level gate (Lv 100). Requires surviving a grueling multi-round boss gauntlet. 
  - *Boss AI Scripts & Anti-Exploit Mechanics*: Boss AI should be highly deterministic rather than purely random. Endgame bosses actively monitor their own resource (SP) pools; if a player attempts to use an SP-drain item to bypass mechanics, an override function triggers unleashing a geometrically scaled squad-wipe attack.
- **Jobs / Specializations**: After the Rebirth gauntlet, the system checks primary stats (e.g., STR vs INT). A physical bias validates unlocking Fighter archetypes; a magical bias unlocks Mage archetypes. Grants specific stat-scaling artifacts equivalent to Job Capes.
  - *Stat Min-Maxing*: The stat validation is a one-time check at the exact moment of Rebirth, allowing players to manipulate stats (e.g., building Speed, keeping STR just 1 point over INT to get a Fighter class, but playing as a Mage post-rebirth).
  - *Hidden Passives*: Certain classes have mathematically hidden defensive mitigation (e.g., 30% reduction scaling on Wisdom) or unique synergies with specific mount types.

### 7. Alchemy & Compounding Math
- **Item Rank Formula**: Equivalent to exactly one-half (0.5x) of the item's equippable character level.
- **Base Groups**: Items are categorized by material (Titanium, Wood, Flower, etc.).
- **Skill Levels**: Primary (+4 max rank jump, -8 drop on failure), Junior (+4 max, -8 drop), Superior (+5 max rank jump, -7 drop on failure).
- **Alchemy Books 1-4**: Artificially reduce the Effective Rank Jump target. *Effective Rank = Target Rank Jump - Book Value*. Achiving exactly 0 Effective Rank jump guarantees 100% success mapping to the base tier probabilities.
- **Garbage Generation**: Catastrophic sub-minimum rank failures universally generate Common Stone or Straw Mushrooms.

### 8. Death & Degradation
- **Death**: -1% of current accumulated EXP towards the next level. Crystal of Efforts reduces this by 50% for 8 hours real-time.
- **Equipment Breakage**: Highly punitive check if `Level_Mob >= Level_Player + 15`. At 0 durability, item enters "broken" state granting zero stats until specifically repaired.
