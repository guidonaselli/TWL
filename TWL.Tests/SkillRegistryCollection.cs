using Xunit;
using TWL.Shared.Domain.Skills;

namespace TWL.Tests;

[CollectionDefinition("SkillRegistry")]
public class SkillRegistryCollection : ICollectionFixture<SkillRegistryFixture>
{
}

public class SkillRegistryFixture : IDisposable
{
    public SkillRegistryFixture()
    {
        // Initial setup if needed
    }

    public void Dispose()
    {
        SkillRegistry.Instance.ClearForTest();
    }
}
