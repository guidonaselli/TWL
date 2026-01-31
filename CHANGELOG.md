# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Missing to Production (Prioritized)
- **Persistence**: Migration from `FilePlayerRepository` (JSON) to PostgreSQL (Critical).
- **Content**: Fix 7 failing Validation Tests (Quests, Localization keys).
- **Economy**: Hybrid Market System (Centralized Ledger + Player Stalls).
- **World**: Instance Isolation (Dungeon copies) + Daily Lockouts (5/day).
- **Social**: Party (Formation, Loot) and Guild Systems (Roster, Chat, Storage).
- **Security**: Authoritative Movement Validation & Anti-Cheat Handshakes.

### Added
- **Docs**: `PRODUCTION_GAP_ANALYSIS.md` detailing technical risks and feature matrix.
- **Docs**: `docs/rules/GAMEPLAY_CONTRACTS.md` defining strict SSOT for Market, PvP, Death, and Instances.
- **Docs**: Domain-specific changelogs in `docs/core`, `docs/skills`, `docs/quests`, etc.

### Changed
- **Tests**: Content Validation logic updated to better detect missing keys.

## [0.1.0] - 2024-05-01
### Added
- Initial Vertical Slice implementation.
- Basic Combat System (Turn-based).
- Quest Engine (Linear chains).
- Pet System (Basic Capture & Stats).
- File-based Persistence.
