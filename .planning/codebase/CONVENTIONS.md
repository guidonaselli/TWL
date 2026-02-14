# Coding Conventions

**Analysis Date:** 2026-02-14

## Naming Patterns

**Files:**
- PascalCase for all filenames (e.g., `CombatManager.cs`, `ServerCharacter.cs`)
- One primary public class per file
- File names match class name

**Functions/Methods:**
- PascalCase for public methods (e.g., `UseSkill()`, `RegisterCombatant()`, `StartEncounter()`)
- camelCase for local functions (rare, typically inline)
- Verb-noun pattern for actions (e.g., `GetCombatant()`, `AddStatusEffect()`, `TryProgress()`)

**Variables:**
- camelCase for local variables and parameters (e.g., `attacker`, `primaryTarget`, `skillId`)
- PascalCase for properties (e.g., `IsAlive`, `MaxHealth`, `SkillId`)

**Types:**
- PascalCase for classes, enums, interfaces, structs (e.g., `Character`, `Team`, `BattleState`)
- Interface names prefixed with `I` (e.g., `ISkillCatalog`, `ICombatant`, `IRandomService`)
- Enum members use PascalCase without prefix (e.g., `enum Team { Player, Enemy }`)

**Private/Protected Fields:**
- camelCase with leading underscore (e.g., `_combatants`, `_random`, `_skillCatalog`)
- Enforced via `.editorconfig` rule: `dotnet_naming_rule.private_fields_should_be_camel_case`

**Constants:**
- PascalCase for public constants (e.g., `const int TestSkillId = 999;`)
- Can also use UPPER_SNAKE_CASE in some contexts (e.g., `GS_WATER_SHRINK = 2001`)
- Defined in dedicated Constants files: `TWL.Shared/Constants/SkillIds.cs`

## Code Style

**Formatting:**
- 4 spaces per indentation level (configured in `.editorconfig`)
- Max line length: 120 characters
- CRLF line endings
- No trailing whitespace (enforced: `trim_trailing_whitespace = false` - but convention is to avoid it)

**Linting:**
- Configured via `.editorconfig` file (IntelliJ Rider format)
- No external linter (eslint/StyleCop) enforced, but EditorConfig rules guide formatting
- Editor: JetBrains Rider (IntelliJ IDE settings in `.idea/`)

**Brace Placement:**
- Allman style (opening brace on new line):
  ```csharp
  if (condition)
  {
      // code
  }
  else
  {
      // code
  }
  ```
- Configured: `csharp_new_line_before_open_brace = all`

**Namespace Declarations:**
- File-scoped namespaces: `namespace TWL.Server.Simulation.Managers;` (C# 10+)
- Single `namespace` declaration per file
- No nested namespaces

## Import Organization

**Order:**
1. System namespaces (`using System;`)
2. Microsoft namespaces (`using Microsoft.Xna.Framework;`)
3. Third-party namespaces (`using Moq;`)
4. Project namespaces (`using TWL.Shared.Domain.Battle;`)

**Path Aliases:**
- Not used. Full qualified namespaces throughout
- Example: `TWL.Shared.Domain.Characters`, `TWL.Server.Simulation.Managers`

**Global Usings:**
- Defined in `GlobalUsings.cs` per project
- Example in `TWL.Tests/GlobalUsings.cs`: `global using Xunit;`
- Avoids repetitive imports across test files

## Error Handling

**Patterns:**
- **Null checks**: Use null-coalescing (`??`) and null-propagation (`?.`) when appropriate
  ```csharp
  var skill = _skills.GetSkillById(request.SkillId);
  if (skill == null)
  {
      return new List<CombatResult>();
  }
  ```
- **List/dictionary checks**: Return empty collections instead of null
  ```csharp
  return new List<CombatResult>(); // instead of null
  return _combatants.TryGetValue(id, out var combatant) ? combatant : null;
  ```
- **Validation exceptions**: Throw `InvalidOperationException` for validation failures
  ```csharp
  throw new InvalidOperationException($"Quest validation failed in {path} with {errors.Count} errors.");
  ```
- **Console logging**: Used in managers for runtime diagnostics
  ```csharp
  Console.WriteLine($"Warning: Duplicate QuestId {def.QuestId} in {path}. Overwriting.");
  ```

## Logging

**Framework:** `Console` (no abstraction layer)

**Patterns:**
- Used in managers for initialization diagnostics: `ServerQuestManager`, `PetManager`
- Example: `Console.WriteLine($"Total Loaded Quests: {_questDefinitions.Count}");`
- Warnings for recoverable issues: `"Warning: Duplicate..."`, `"Warning: Quest path not found"`
- No structured logging framework (Serilog, NLog)

## Comments

**When to Comment:**
- XML documentation on public classes and methods (rare but present in managers)
- Example from `CombatManager.cs`:
  ```csharp
  /// <summary>
  ///     CombatManager vive en el servidor. Gestiona turnos y el cálculo de daño real.
  /// </summary>
  ```
- Inline comments explain complex logic or non-obvious behavior:
  ```csharp
  // Set Mock to 0.0 -> Variance should be 0.95
  var mockRandom = new MockRandomService(0.0f);
  ```
- Comments describe test arrangement: `// Arrange`, `// Act`, `// Assert` pattern

**JSDoc/TSDoc:**
- Not applicable (C# project, not TypeScript)
- XML documentation (`///`) used sparingly for public API

## Function Design

**Size:** Methods typically 20-100 lines
- Larger methods (100+) in complex managers: `ServerQuestManager.LoadFile()` handles JSON parsing, validation, error reporting
- Small utility methods (5-20 lines) in helpers and resolvers

**Parameters:**
- Use positional parameters with clear names
- Constructor parameters often stored as readonly fields with underscore prefix
- Example from `CombatManager`:
  ```csharp
  public CombatManager(ICombatResolver resolver, IRandomService random, ISkillCatalog skills,
      IStatusEngine statusEngine)
  {
      _resolver = resolver;
      _random = random;
      _skills = skills;
      _statusEngine = statusEngine;
  }
  ```

**Return Values:**
- Methods return `bool` for success/failure checks
- Return empty collections (`new List<>()`) instead of null
- Use nullable return types (`Type?`) for optional results
- Generic return type pattern: `List<CombatResult>`, `List<ServerCharacter>`

## Module Design

**Exports:**
- Public classes and interfaces are primary exports
- Private/internal members use underscore convention
- No barrel files; explicit imports required

**Access Modifiers:**
- Explicit modifier requirement: `dotnet_style_require_accessibility_modifiers = for_non_interface_members:suggestion`
- Interfaces: members implicitly public
- Classes: explicitly mark public/private/protected/internal
- Sealed classes used for non-inheritable types

**Inheritance:**
- Abstract base classes used for shared behavior: `Character` (base for game entities)
- Interfaces used for contracts: `ISkillCatalog`, `ICombatant`, `IRandomService`
- Virtual methods for overrideable behavior: `public virtual void RegisterCombatant()`

**Dependency Injection:**
- Constructor injection standard pattern
- Services accept interface types, not concrete implementations
- Example: `CombatManager` accepts `ICombatResolver`, `IRandomService`, `ISkillCatalog`
- Mocking in tests relies on this DI pattern with Moq

## Expression-Bodied Members

**Used for:**
- Property getters with simple logic:
  ```csharp
  public int Atk => Str * 2;
  public bool IsAlive() => Health > 0;
  ```
- Short helper methods

**Not used for:**
- Complex method bodies (explicit blocks instead)
- Constructors (always use block syntax)

## Type Preferences

**Var keyword:**
- `var` preferred when type is obvious: `var skill = new Skill { ... };`
- Explicit types for clarity in complex scenarios
- Configured: `csharp_style_var_for_built_in_types = true:suggestion`

**Modern patterns:**
- Object initializers used liberally:
  ```csharp
  var def = new PetDefinition
  {
      PetTypeId = 100,
      Name = "Test Pet",
      Element = Element.Earth
  };
  ```
- Collection initializers: `new List<int> { 1, 2, 3 }`
- Null coalescing: `FixedFloat ?? 0.5f`

## Code Organization Within Files

1. **Using statements** (sorted: System, Microsoft, third-party, project)
2. **File-scoped namespace**
3. **Class/interface declaration**
4. **Constants** (if any)
5. **Fields** (private with underscore)
6. **Properties** (public, auto-properties preferred)
7. **Events** (public event declarations)
8. **Constructors** (including overloads)
9. **Public methods** (in logical order)
10. **Protected/private methods**
11. **Nested types** (if any)

Example from `CombatManager.cs`:
```csharp
public class CombatManager
{
    // Fields
    private readonly ConcurrentDictionary<int, ServerCombatant> _combatants;
    private readonly ConcurrentDictionary<int, ITurnEngine> _encounters = new();

    // Events
    public event Action<ServerCombatant>? OnCombatantDeath;

    // Constructors
    public CombatManager(...) { }

    // Public methods
    public virtual void RegisterCombatant(...) => ...

    // Private helpers
    private void InternalMethod(...) { }
}
```

## Thread Safety

**Collections:**
- `ConcurrentDictionary<int, T>` used for shared state in managers
- Example: `_combatants`, `_encounters` in `CombatManager`
- TryGetValue/TryRemove patterns for safe concurrent access

**No explicit locking:**
- Relies on concurrent collections and immutable patterns
- Events are thread-safe by design (delegates)

---

*Convention analysis: 2026-02-14*
