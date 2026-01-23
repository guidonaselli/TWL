# Design: Fire Element Skills (T1/T2) & Core Combat Mechanics

## Purpose
To complete the Fire element skill set for Tier 1 and Tier 2, providing a complete "package" of Physical, Magical, and Support skills. Additionally, to implement core systemic mechanics (Elemental Counters and Control Hit Chance) to deepen combat strategy.

## Fire Element Overview
Fire focuses on **Damage (Atk/Mat)** and **DoT (Burn)**. It lacks hard control or defense but excels at pressure.

### New Skills

#### 1. Ignite Spirit (ID 24)
- **Branch:** Support
- **Tier:** 1
- **Element:** Fire
- **Description:** Channels fire energy to boost an ally's attack power.
- **Target:** Single Ally
- **Cost:** 15 SP
- **Effect:** BuffStats (Atk) +20% (approx, value driven) for 3 turns.
- **Scaling:** Wis * 1.0 (Value).
- **Justification:** Completes the Support branch for Fire. Fire supports should boost offense.

#### 2. Lava Strike (ID 25)
- **Branch:** Physical
- **Tier:** 2
- **Element:** Fire
- **Description:** A heavy weapon strike that splashes lava, causing burn.
- **Target:** Single Enemy
- **Cost:** 20 SP
- **Effect:** Damage (High) + Burn (Chance).
- **Scaling:** Atk * 1.3 + Str * 0.5.
- **Burn:** Value 10, Duration 2, Chance 0.5.
- **Justification:** Provide a T2 Physical option that synergizes with the DoT theme.

## Core Mechanics

### Elemental Counters
The system enforces a cyclic advantage: **Earth > Water > Fire > Wind > Earth**.

- **Formula:** `Multiplier = GetElementalMultiplier(SkillElement, TargetElement)`
- **Strong Matchup (e.g., Fire Skill vs Wind Target):** 1.5x Damage.
- **Weak Matchup (e.g., Fire Skill vs Water Target):** 0.5x Damage.
- **Neutral Matchup (e.g., Fire Skill vs Earth Target):** 1.0x Damage.
- **Application:** Applied to final calculated damage before defense reduction (or after, depending on preference for flat defense interaction).
  - Decision: Apply **after** scaling but **before** defense to reward hitting weaknesses against high-def targets? Or after net damage?
  - WLO style often acts as a final modifier. Let's apply it to the "Potential Damage" (`totalValue`) before Defense. This makes Elemental Advantage help penetrate Defense.

### Control Hit Chance
Auxiliary skills (Seal, Debuff) must have a hit chance based on INT vs WIS.

- **Formula:** `Chance = BaseChance + (Attacker.Int - Defender.Wis) * 0.01`
- **Constraints:**
  - Min Chance: 10% (Always a slight chance)
  - Max Chance: 100% (Guaranteed if gap is huge)
- **Application:**
  - `Seal` effects.
  - `DebuffStats` effects.
  - Note: `Burn` (DoT) usually relies on the skill's base chance, but for consistency, we can apply it there too, or keep it fixed for Physical skills. The prompt specifies "auxiliares tipo seal". I will focus this logic on `Seal` and `DebuffStats` tags primarily.
