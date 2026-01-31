# Core System Changelog

## [Unreleased]

### Missing
- **Persistence**: `FilePlayerRepository` is a prototype. Must implement `PostgresPlayerRepository`.
- **Security**: `MovementValidator` is missing.
- **Networking**: Packet replay protection is missing.

### Existing
- **Networking**: `ClientSession` handles basic packet routing.
- **Persistence**: `FilePlayerRepository` handles atomic file moves (Safe-ish for single user, unsafe for cluster).
- **Concurrency**: Basic thread-safety in `ServerCharacter` (Interlocked/Locks).
