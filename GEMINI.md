# GEMINI.md — The Wonderland Legacy (TWL)

> Project-level instructions for Gemini CLI. Read before acting.
> For full project context, architecture, and GSD protocol, see `AGENTS.md` and `CONTEXT.md`.

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

## GSD Protocol

Follow the TWL Continuum Rule — full spec in `.agents/rules/twl-gsd.md` and `AGENTS.md`.

1. **Sync-Check**: Read `.planning/STATE.md` + `.planning/ROADMAP.md` before any work.
2. **Gap Detection**: New needs → append to `REQUIREMENTS.md` under `## v1.1 Discovered Requirements`. Do not act on them immediately.
3. **Hard Commit**: Task is DONE only when code passes tests + `ROADMAP.md` marked `[x]` + `STATE.md` updated.
4. **Momentum**: After completion, propose the next pending task.

---

## Code Conventions

- **Language**: C# 12 / .NET 10
- **Architecture**: Server-authoritative (TWL.Shared → TWL.Client / TWL.Server)
- **Naming**: PascalCase public, `_camelCase` private fields
- **Testing**: xUnit in `TWL.Tests/`, InMemory repos for unit tests
- **Commits**: `feat(phase-N): description`

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
