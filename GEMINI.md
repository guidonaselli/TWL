# GEMINI.md — The Wonderland Legacy (TWL)

> Project-level instructions for Gemini CLI and Jules. Read before acting.
> For full project context, architecture, and GSD protocol, see `AGENTS.md` and `.gsd/PROJECT.md`.

---

## GSD 2 State Protocol

**All state lives in `.gsd/`, NOT `.planning/` (legacy, do not use).**

| File | Purpose |
|------|---------|
| `.gsd/STATE.md` | Current position: active milestone, slice, task |
| `.gsd/PROJECT.md` | Living project description |
| `.gsd/REQUIREMENTS.md` | All requirements with status tracking |
| `.gsd/DECISIONS.md` | Append-only architectural decisions register |
| `.gsd/milestones/M001/M001-ROADMAP.md` | Master slice checklist for Milestone 1 |
| `.gsd/milestones/M001/slices/SXX/SXX-PLAN.md` | Task checklist per slice |
| `.gsd/milestones/M001/slices/SXX/tasks/TYY-PLAN.md` | Individual task implementation plan |

### Sync-Check (Mandatory Before Any Work)

1. Read `.gsd/STATE.md` → find active milestone + slice + task
2. Read `.gsd/milestones/M001/M001-ROADMAP.md` → find first `- [ ]` slice in your domain
3. Read that slice's plan → find first `- [ ]` task
4. Read that task's plan → understand implementation scope
5. Read `.gsd/REQUIREMENTS.md` → understand why the task matters

### Completion Protocol

1. Tests pass: `pwsh -File scripts/verify.ps1`
2. Task marked `[x]` in slice plan file
3. Summary written: `tasks/TXX-SUMMARY.md`
4. If slice fully done → mark `[x]` in `M001-ROADMAP.md`
5. Update `.gsd/STATE.md`

---

## Shell Commands Convention

**Never construct inline shell pipelines** like `dotnet test ... 2>&1 | tail -30`.

Always use the stable PowerShell scripts in `scripts\` via:

```
pwsh -File scripts\<script>.ps1 [args]
```

| Script | Purpose | Example |
|--------|---------|---------|
| `scripts\verify.ps1` | restore → build → test | `pwsh -File scripts\verify.ps1` |
| `scripts\build.ps1` | restore + build only | `pwsh -File scripts\build.ps1 -Config Debug` |
| `scripts\test-filter.ps1` | filtered xUnit test run | `pwsh -File scripts\test-filter.ps1 -Names CharacterRebirthTransactionTests -NoBuild -Tail 30` |
| `scripts\read-log.ps1` | read any file (log/txt/md/trx/json) | `pwsh -File scripts\read-log.ps1 -Path test.log -Tail 50` |
| `scripts\read-runner-info.ps1` | smart lookup of test results and logs | `pwsh -File scripts\read-runner-info.ps1 -Type trx` |

Known file locations:
- Test results (TRX): `TWL.Tests/TestResults/`
- Root logs: `*.log`
- Artifacts: `artifacts/*.json`
- Server logs: `TWL.Server/Logs/`

---

## Code Conventions

- **Language**: C# 12 / .NET 10
- **Architecture**: Server-authoritative (TWL.Shared → TWL.Client / TWL.Server)
- **Naming**: PascalCase public, `_camelCase` private fields
- **Testing**: xUnit in `TWL.Tests/`, InMemory repos for unit tests
- **Commits**: `feat(SXX): TYY - description` or `content(SXX): TYY - description`

---

## Engineering Rules

### Universal

**Fix root causes, not symptoms.**
Trace failures to their origin and fix them there. Never add null-checks, try/catch, or guards that hide the real problem. One fix at the source beats ten patches on the surface.

**Make invalid states unrepresentable.**
Use enums over magic ints. Use non-nullable types over defensive null-checks everywhere. If wrong usage doesn't compile, it can't reach production.

**Fail loudly at system boundaries.**
Validate at edges (incoming network packets, JSON file loads, user input). Trust your own types internally. One `throw` at the door eliminates ten `?.` chains inside.

**One responsibility per unit.**
If you need "and" to describe what a method or class does, split it. Methods fit on screen. Classes have one reason to change.

**No silent failures.**
Never swallow exceptions with an empty `catch {}`. Handle meaningfully, log-and-rethrow, or let propagate. Silence hides bugs.

**Inject dependencies explicitly.**
Classes declare what they need in the constructor. No `Instance` singletons, no `ServiceLocator.Get<T>()`, no mutable statics. Hidden dependencies create invisible coupling.

**Names declare intent, not type.**
`ProcessData()` is not a name. `ApplySkillDamage(skill, target)` is. Variable, method, and class names must say *what they do or represent*.

**No careless allocations in hot paths.**
Profile before optimizing. But `new List<T>()` on every `Update()` frame is not optimization — it's carelessness. Use pools, pre-allocated arrays, or structs where GC pressure matters.

---

### MonoGame / Game Loop

**`Draw()` is read-only.** No state mutations in `Draw()`. It reads game state and renders. Logic belongs in `Update()`.

**`Update()` owns all logic.** Physics, AI, input, timers, state transitions — all here. `Draw()` presents the current frame snapshot only.

**Content loads once, in `LoadContent()`.** Never call `Content.Load<T>()` inside `Update()` or `Draw()`. Asset loading is disk I/O; one call per asset per scene lifetime.

**Frame-rate independence.** All movement, timers, and interpolations use `gameTime.ElapsedGameTime.TotalSeconds`. Never assume a fixed delta.
```csharp
// ❌ position.X += 2f;
// ✅ position.X += speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
```

**SpriteBatch has defined scope.** One `Begin()`, one `End()`. Never open a batch while another is active. Layer via `SpriteSortMode` + `layerDepth`, not nested batches.

**Coordinate systems must be explicit.** World coordinates ≠ screen coordinates. Conversions happen in one place with named methods (`WorldToScreen`, `ScreenToWorld`). Never mix systems implicitly.

**Dispose what you manually create.** `Texture2D`, `RenderTarget2D`, `Effect`, `SoundEffect` are `IDisposable`. If you created it outside `ContentManager`, you own the `Dispose()` — call it in `UnloadContent()` or via `using`.

---

### Architecture (TWL)

**Server is the single source of truth.** All game state mutations happen on `TWL.Server`. The client never directly modifies `PlayerCharacter`. Optimistic UI updates must be clearly provisional and always reconciled.

**`TWL.Shared` has zero MonoGame references.** It is pure domain: types, rules, DTOs. `Texture2D`, MonoGame's `Vector2`, `SpriteBatch` — all belong to `TWL.Client`. Non-negotiable.

**DTOs cross the wire; domain models do not.** Serialize only DTOs. Validate every incoming DTO as if it came from a hostile source.

**Content IDs are stable contracts.** Once assigned, an ID never changes or gets reused. Items: 1–9999 | Monsters: 2000+ | Pets: 1000+ | Quests: 1000+ | Skills: 1000+. ID errors corrupt saves and the economy.

**Game data lives in JSON, not in code.** Skills, items, quests, monsters, pets — defined in `Content/Data/`. C# loads and applies them; it does not define them. Hardcoded stats or content names in C# are architecture bugs.

---

## Content Design Rules

- Progressive difficulty: 8 tiers mapping to Lv1-100
- Elemental coverage: every tier has Earth/Water/Fire/Wind variants
- Rarity distribution: 60% Common, 25% Uncommon, 10% Rare, 4% Epic, 1% Legend
- Pet types: `Capture` (wild), `Quest` (story), `HumanLike` (special NPCs)
- Quest types: Talk, Collect, Kill, Reach, Interact, Craft, Deliver, Instance, UseItem
- Names must be ORIGINAL (not direct WLO copies) but mechanics should mirror WLO depth
- Item economy: costs scale exponentially by tier (Tier 1: 50-500g → Tier 8: 300k-1Mg)

## Map Regions

| ID Range  | Region           | Theme            | Level Range |
|-----------|------------------|------------------|-------------|
| 0001-0099 | Isla Brisa       | Tropical beach   | 1-10        |
| 1000-1099 | Puerto Roca      | Port city/jungle | 10-20       |
| 2000-2099 | Selva Esmeralda  | Deep jungle/ruins| 20-30       |
| 3000-3099 | Arrecife Hundido | Underwater/caves | 30-45       |
| 4000-4099 | Isla Volcana     | Volcanic/lava    | 45-60       |
| 5000-5099 | Cascada Eterna   | Waterfall/mist   | 60-75       |
| 6000-6099 | Cumbre Ancestral | Mountain/ancient | 75-90       |
| 7000-7099 | Resonancia Core  | Crystal/endgame  | 90-100      |
