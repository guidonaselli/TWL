# Skills Roadmap

## Overview
This document outlines the planned progression for skills, organized by element and tier.

## Design Philosophy
- **Earth:** Survival, Control, Mitigation.
- **Fire:** Burst, Pressure, DoT (Burn).
- **Water:** Sustain, Cleanse, Soft Control.
- **Wind:** Speed, Evasion, Disruption.

## Implementation Status

### Fire (In Progress)
- **T1:**
  - Fireball (Mag) - Implemented
  - Flame Strike (Phys) - Implemented
  - **Ignite Spirit** (Support) - Planned
- **T2:**
  - Blaze (Mag) - Implemented
  - Heat Wave (Mag AoE) - Implemented
  - **Lava Strike** (Phys) - Planned

### Water (In Progress)
- **T1:**
  - **Water Ball** (Magical): Basic single target water damage.
  - **Aqua Recover** (Support): Single target heal.
  - **Aqua Impact** (Physical): Basic physical water damage.
- **T2:**
  - **Purification** (Support): Single target cleanse (Removes Burn, DebuffStats, Seal).
- **T3:**
  - *Planned:* Tidal Wave (Magical AoE), Resurrection (Support).

### Earth (In Progress)
- **T1:**
  - **Rock Smash** (Physical): Basic earth damage.
  - **Stone Bullet** (Magical): Single target earth projectile.
  - **Earth Barrier** (Support): Defense Buff (BuffStats).
- **T2:** Earthquake (Mag AoE), Fortify (Support - Shield).

### Wind (Planned)
- **T1:** Wind Blade (Phys), Gale (Mag).
- **T2:** Haste (Support - Spd Buff), Sonic Boom (Phys - Row).

## Technical Requirements
- [x] Basic Damage/Heal Support.
- [x] DoT (Burn) Support.
- [x] Cleanse/Dispel Logic.
- [x] Shield Mechanics.
- [x] Elemental Counters (Earth > Water > Fire > Wind > Earth).
- [x] Control Hit Chance (Int vs Wis).
- [ ] Speed/ATB Manipulation.
