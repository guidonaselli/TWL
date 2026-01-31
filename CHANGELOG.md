# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Missing to Production (Prioritized)
- **Persistence**: Migration from JSON files to PostgreSQL (Critical).
- **Economy**: Hybrid Market System (Centralized Ledger + Player Stalls).
- **World**: Instance Isolation (Dungeon copies) + Daily Lockouts (5/day).
- **Social**: Party and Guild Systems.
- **Security**: Authoritative Movement Validation & Anti-Cheat Handshakes.
- **Content**: Fixes for failing Quest/Skill Validation Tests (8 failures).

### Added
- **Docs**: Comprehensive `GAMEPLAY_CONTRACTS.md` defining strict rules for Market, PvP, and Death.
- **Audit**: `PRODUCTION_GAP_ANALYSIS.md` detailed report.

### Changed
- **Tests**: Content Validation logic updated to better detect missing keys.

## [0.1.0] - 2024-05-01
### Added
- Initial Vertical Slice implementation.
- Basic Combat System (Turn-based).
- Quest Engine (Linear chains).
- Pet System (Basic Capture & Stats).
- File-based Persistence.
