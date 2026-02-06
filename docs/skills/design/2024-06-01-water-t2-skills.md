# Water Element T2 Skill Pack Design
Date: 2024-06-01
Author: Jules

## Overview
This document defines the Tier 2 skills for the Water element, focusing on "Healing & Sustain" identity while expanding into control and debuffs.

## Skills

### 1. Physical: Aqua Crescent (3010)
*   **Concept**: A sweeping strike of water that slows enemies.
*   **Identity**: Control/Debuff via physical damage.
*   **Target**: `RowEnemies` (Front row or selected row).
*   **Unlock**: Parent `Aqua Impact III` (3003), Rank 10.
*   **Cost**: 20 SP, 1 Cooldown.
*   **Scaling**: 1.5x ATK.
*   **Effects**:
    *   `Damage`: Standard calculation.
    *   `DebuffStats`: Param "Spd", Value 10%, Duration 2 turns. Chance 1.0.
*   **Stacking**: `RefreshDuration`.
*   **ConflictGroup**: `Debuff_Spd`.

### 2. Magical: Frost Bite (3110)
*   **Concept**: A freezing lance of ice that can seal the target.
*   **Identity**: Hard Control (Seal).
*   **Target**: `SingleEnemy`.
*   **Unlock**: Parent `Water Ball III` (3103), Rank 10.
*   **Cost**: 25 SP, 3 Cooldown.
*   **Scaling**: 1.6x MAT.
*   **Effects**:
    *   `Damage`: Standard calculation.
    *   `Seal`: Duration 2 turns. Base Chance 0.6.
*   **HitRules**: Base 0.6, StatDependence `Int-Wis`. Min 0.1, Max 0.9.
*   **ResistanceTags**: `SealResist`.
*   **Stacking**: `RefreshDuration`.
*   **ConflictGroup**: `HardControl`.
*   **Outcome**: `Resist`.

### 3. Support: Soothing Mist (3210)
*   **Concept**: A gentle mist that cleanses impurities and heals wounds.
*   **Identity**: Sustain + Cleanse.
*   **Target**: `AllAllies`.
*   **Unlock**: Parent `Aqua Recover III` (3203), Rank 10.
*   **Cost**: 40 SP, 3 Cooldown.
*   **Scaling**: 2.0x WIS.
*   **Effects**:
    *   `Cleanse`: Remove all debuffs.
    *   `Heal`: Standard calculation (AoE might reduce coef? keeping 2.0x as T2 cost is high).
*   **Stacking**: N/A (Instant).

## Systemic Integration
*   **Auto-Battle**:
    *   `Soothing Mist` should be prioritized when multiple allies have debuffs (Cleanse logic).
    *   `Frost Bite` should be used against high-threat targets (Seal logic).
*   **Resistance**: `Frost Bite` respects `SealResist`.
