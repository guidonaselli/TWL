# Testing Patterns

**Analysis Date:** 2026-02-14

## Test Framework

**Runner:**
- xUnit 2.9.3
- Config: `TWL.Tests/TWL.Tests.csproj` (Microsoft.NET.Test.Sdk 18.0.1)

**Assertion Library:**
- xUnit built-in assertions (no separate Fluent Assertions library)
- Examples: `Assert.Equal()`, `Assert.Single()`, `Assert.NotNull()`, `Assert.True()`, `Assert.Contains()`

**Mocking:**
- Moq 4.18.4 for interface mocking

**Coverage:**
- coverlet.collector 6.0.4 for code coverage measurement
- No enforced coverage targets (configured but not strict)

**Run Commands:**
```bash
dotnet test                          # Run all tests
dotnet test --watch                  # Watch mode (if supported by SDK)
dotnet test /p:CollectCoverage=true  # Generate coverage report
```

## Test File Organization

**Location:**
- Co-located in separate project: `TWL.Tests/` (not alongside source)
- Mirrors source structure: `TWL.Tests/Combat/`, `TWL.Tests/Services/`, `TWL.Tests/Domain/`, etc.

**Naming:**
- Convention: `{ComponentName}Tests.cs` (e.g., `CombatManagerTests.cs`, `PetServiceTests.cs`)
- Test classes named: `public class {Name}Tests`
- Test methods named: `public void {Action}_Should{Expectation}` (e.g., `UseSkill_CalculatesDamage_WithVariance_Normal`)

**Structure:**
```
TWL.Tests/
├── Combat/                    # Combat system tests
├── Characters/                # Character domain tests
├── Domain/                    # Shared domain logic tests
├── Services/                  # Service layer tests
├── Skills/                    # Skill mechanics tests
├── Quests/                    # Quest system tests
├── Mocks/                     # Custom mock implementations
├── ContentValidationTests.cs  # Root level for integration tests
└── GlobalUsings.cs            # Shared global usings (Xunit)
```

## Test Structure

**Suite Organization:**
```csharp
public class CombatManagerTests
{
    [Fact]
    public void UseSkill_CalculatesDamage_WithVariance_Normal()
    {
        // Arrange
        var mockRandom = new MockRandomService(0.5f);
        var catalog = CreateMockCatalog();
        var resolver = new StandardCombatResolver(mockRandom, catalog);
        var manager = new CombatManager(resolver, mockRandom, catalog, new StatusEngine());

        var attacker = new ServerCharacter { Id = 1, Name = "Attacker", Str = 100, Agi = 50, Hp = 1000 };
        var target = new ServerCharacter { Id = 2, Name = "Target", Hp = 1000, Con = 0, Team = Team.Enemy, Agi = 10 };

        manager.StartEncounter(1, new List<ServerCharacter> { attacker, target });

        // Act
        var request = new UseSkillRequest { PlayerId = 1, TargetId = 2, SkillId = 999 };
        var result = manager.UseSkill(request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(200, result[0].Damage);
    }
}
```

**Patterns:**
- **Arrange-Act-Assert (AAA)**: All tests follow this pattern with explicit comment headers
- **Setup helper methods**: Private `Create*()` methods for common test object construction
  ```csharp
  private ISkillCatalog CreateMockCatalog()
  {
      var catalog = new MockSkillCatalog();
      var skill = new Skill { /* ... */ };
      catalog.AddSkill(skill);
      return catalog;
  }
  ```
- **Inline object initialization**: Uses C# object initializers for test data
  ```csharp
  var attacker = new ServerCharacter
  {
      Id = 1, Name = "Attacker", Str = 100, Agi = 50, Hp = 1000
  };
  ```
- **Teardown**: Implicit (no setup/teardown code) unless test implements `IDisposable`
  ```csharp
  public class PetServiceTests : IDisposable
  {
      public void Dispose()
      {
          // Cleanup temporary files created in tests
          File.Delete(TestFile);
      }
  }
  ```

## Mocking

**Framework:** Moq 4.18.4

**Custom Mocks (No Moq):**
- `MockRandomService` in `TWL.Tests/Mocks/MockRandomService.cs` - Custom implementation for deterministic random values
  ```csharp
  public class MockRandomService : IRandomService
  {
      public float FixedFloat { get; set; } = 0.5f;
      public int FixedInt { get; set; } = 0;

      public float NextFloat(string? context = null) => FixedFloat;
      public int Next(string? context = null) => FixedInt;
  }
  ```
- `MockSkillCatalog` in `TWL.Tests/Mocks/MockSkillCatalog.cs` - Simple dictionary-based skill catalog
  ```csharp
  public class MockSkillCatalog : ISkillCatalog
  {
      private readonly Dictionary<int, Skill> _skills = new();
      public void AddSkill(Skill skill) => _skills[skill.SkillId] = skill;
  }
  ```

**Moq Usage Pattern:**
```csharp
private readonly Mock<IRandomService> _mockRandom;
private readonly Mock<ICombatResolver> _mockResolver;
private readonly Mock<ISkillCatalog> _mockSkills;

public void SetUp()
{
    _mockRandom = new Mock<IRandomService>();
    _mockResolver = new Mock<ICombatResolver>();

    // Setup return values
    _mockRandom.Setup(x => x.NextFloat(It.IsAny<string?>())).Returns(0.0f);
    _mockResolver.Setup(x => x.Resolve(...)).Returns(...);
}
```

**What to Mock:**
- External services: `IRandomService`, `ICombatResolver`, `ISkillCatalog` (when testing other logic)
- Repository interfaces: `IPlayerRepository`
- Complex dependencies that aren't being tested
- Services with side effects

**What NOT to Mock:**
- Domain entities: `ServerCharacter`, `Skill`, `QuestDefinition`
- Value objects: `StatusEffectInstance`, `UseSkillRequest`
- Real implementations of core logic being tested: Use real `CombatManager`, `StatusEngine` when testing combat
- Utility classes and enums

## Fixtures and Factories

**Test Data:**
- **Inline creation** in most tests using object initializers
- **File-based fixtures** for large datasets: JSON files in `Content/Data/` directory
  ```csharp
  var qm = new ServerQuestManager();
  qm.Load("Content/Data/quests.json");
  ```
- **Builder pattern** implicit through constructor parameters:
  ```csharp
  var def = new PetDefinition
  {
      PetTypeId = 100,
      Name = "Test Pet",
      Element = Element.Earth,
      BaseHp = 100,
      // ... more properties
  };
  ```

**Location:**
- No dedicated fixture classes
- Test data created inline in test methods
- Constants for repeated test values at class level: `private const string TestSkillJson = @"[...]";`
- Helper files for complex setup: `ContentTestHelper.cs`, `ContentValidationTests.cs` provide `LoadSkills()`, `LoadQuests()` methods

**Factories in Tests:**
- `ContentTestHelper` in `TWL.Tests/ContentTestHelper.cs`:
  ```csharp
  public static List<Skill> LoadSkills() { /* loads from JSON */ }
  public static List<QuestDefinition> LoadQuests() { /* loads from JSON */ }
  ```
- Prevents duplication across content validation tests

## Coverage

**Requirements:** Not enforced (no CI gate)

**View Coverage:**
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

**Coverage Tools:**
- coverlet.collector - Integrated test coverage
- Output: Coverage reports in standard Cobertura format

## Test Types

**Unit Tests:**
- Scope: Single class/method in isolation with mocked dependencies
- Example: `CombatManagerTests.UseSkill_*` - Tests damage calculation with fixed random values
- Pattern: Arrange test objects, Act with single method call, Assert results

**Integration Tests:**
- Scope: Multiple components working together
- Example: `QuestSystemTests.BasicChain_ShouldWork_EndToEnd()` - Tests full quest flow with real file loading
  ```csharp
  [Fact]
  public void BasicChain_ShouldWork_EndToEnd()
  {
      var qm = new ServerQuestManager();
      qm.Load("Content/Data/quests.json");  // Real file, not mock
      var pq = new PlayerQuestComponent(qm); // Real components

      Assert.True(pq.CanStartQuest(1001));
      Assert.True(pq.StartQuest(1001));
      // ... verify full flow
  }
  ```
- Load real JSON content files and verify system behavior end-to-end

**Content Validation Tests:**
- Scope: Data integrity and consistency checks
- Files: `ContentValidationTests.cs` (617 lines)
- Example: `ValidateSkillCategories()` - Ensures all skills have valid categories
  ```csharp
  [Fact]
  public void ValidateSkillCategories()
  {
      var skills = ContentTestHelper.LoadSkills();
      foreach (var skill in skills)
      {
          Assert.Contains(skill.Category, ContentRules.ValidCategories);
      }
  }
  ```
- Validates against `ContentRules` (business rules for content)

**E2E Tests:**
- Not formally organized as E2E suite
- Some tests act as E2E: `QuestSystemTests`, `CombatManagerConcurrencyTests`
- Server-side logic testing; no UI automation framework used

**Performance Tests:**
- Directory: `TWL.Tests/Benchmarks/`
- Framework: BenchmarkDotNet style tests (but using xUnit with timing)
- Examples: `InventoryBenchmarkTests.cs`, `JsonBenchmarkTests.cs`
- Measure execution time of critical paths

## Common Patterns

**Async Testing:**
- Async tests not used (xUnit supports `[Fact] async Task`)
- Code is synchronous; no Task-based testing needed

**Error Testing:**
- Verify exceptions thrown:
  ```csharp
  [Fact]
  public void StartQuest_ShouldFail_WhenRequirementsNotMet()
  {
      var result = _playerQuests.StartQuest(2); // Requires quest 1 completed
      Assert.False(result);
      Assert.False(_playerQuests.QuestStates.ContainsKey(2));
  }
  ```
- Verify error conditions return expected values (not exception-based)
- No `Assert.Throws<>()` pattern heavily used; prefer return value checks

**State Verification:**
- Tests verify object state after operations:
  ```csharp
  _playerQuests.StartQuest(1);
  _playerQuests.UpdateProgress(1, 0, 1);
  Assert.Equal(1, _playerQuests.QuestProgress[1][0]); // Check progress array
  ```
- Inspect internal dictionaries and lists for correctness

**Parameterized Tests:**
- Not heavily used
- Would use `[Theory]` with `[InlineData()]` if needed
- Example usage not found in codebase; tests prefer separate `[Fact]` methods

**Test Data Builders:**
- Not used; inline object initialization preferred
- Constructor chaining with optional parameters for variation

## Test Comments

**Pattern:** Comments explain non-obvious test behavior
```csharp
// MockRandomService defaults to 0.5f -> Variance 1.0
var mockRandom = new MockRandomService(0.5f);

// Base Damage = 200
// Variance = 1.0
// Final Damage = 200 - 0 = 200
var result = manager.UseSkill(request);

// Attacker (Id 1) should go second because Target (Id 2) has higher Agi
```

## Test Isolation

**Setup/Teardown:**
- Constructor used for setup when needed
- `IDisposable` for cleanup:
  ```csharp
  public class PetServiceTests : IDisposable
  {
      private const string TestFile = "Content/Data/pets_service_test.json";

      public void Dispose()
      {
          File.Delete(TestFile);
      }
  }
  ```
- Temporary test files created in `Content/Data/` directory
- File cleanup in Dispose() ensures no test pollution

**Test Independence:**
- Each test creates its own objects
- No shared state between tests
- No test method ordering dependencies

## Known Test Gaps

**Not Tested:**
- Client-side code (UI logic) - No UI tests
- Networking layer integration - Mock-based testing only
- Full server deployment scenarios - No integration with actual network

---

*Testing analysis: 2026-02-14*
