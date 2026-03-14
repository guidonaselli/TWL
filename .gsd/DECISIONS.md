# Decisions

<!-- Append-only register of architectural and pattern decisions -->

| ID | Decision | Rationale | Date |
|----|----------|-----------|------|
| D001 | Maintain server-authoritative architecture across all migrated GSD slices | This is a core architectural contract in the old project description and underpins anti-cheat, authoritative combat, and multiplayer consistency | 2026-02-14 |
| D002 | Preserve the 3-layer separation (`TWL.Shared` / `TWL.Client` / `TWL.Server`) | The source project summary treats this as validated and foundational for maintainability and future client evolution | 2026-02-14 |
| D003 | Keep gameplay content data-driven in JSON instead of moving it into C# | Skills, quests, pets, items, equipment, and monsters are explicitly described as JSON-driven contracts in the source artifacts | 2026-02-14 |
| D004 | Use PostgreSQL as the production persistence target | Old roadmap and requirements identify file-based persistence as a blocker for concurrency, transactions, and launch-readiness | 2026-02-15 |
| D005 | Use EF Core for complex writes and migrations, with Dapper for high-performance reads | This hybrid persistence strategy is repeated consistently across roadmap, requirements, and research artifacts | 2026-02-15 |
| D006 | Treat replay protection, movement validation, idempotency, and serializable transactions as mandatory foundations for valuable multiplayer operations | The old planning materials frame these as security prerequisites, not optional polish | 2026-02-15 |
| D007 | Preserve turn-based combat and the elemental cycle as core gameplay identity | The project summary marks these as validated differentiators rather than experimental mechanics | 2026-02-14 |
