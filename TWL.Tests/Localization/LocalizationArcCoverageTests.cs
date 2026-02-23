using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace TWL.Tests.Localization;

public class LocalizationArcCoverageTests
{
    private readonly HashSet<string> _baseKeys = new();
    private readonly HashSet<string> _enKeys = new();

    public LocalizationArcCoverageTests()
    {
        var solutionRoot = FindSolutionRoot();
        var clientPath = Path.Combine(solutionRoot, "TWL.Client");
        
        LoadResourceKeys(Path.Combine(clientPath, "Resources", "Strings.resx"), _baseKeys);
        LoadResourceKeys(Path.Combine(clientPath, "Resources", "Strings.en.resx"), _enKeys);
    }
    
    private void LoadResourceKeys(string path, HashSet<string> keys)
    {
        if (File.Exists(path))
        {
            var doc = XDocument.Load(path);
            foreach (var data in doc.Descendants("data"))
            {
                var key = data.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(key))
                {
                    keys.Add(key);
                }
            }
        }
    }

    [Theory]
    [InlineData("QUEST_1301", "Hidden Ruins Intro")]
    [InlineData("QUEST_1302", "Hidden Ruins Core")]
    [InlineData("QUEST_1303", "Hidden Ruins Depth")]
    [InlineData("QUEST_1304", "Hidden Ruins Artifact")]
    [InlineData("QUEST_1305", "Hidden Ruins Riddle")]
    [InlineData("QUEST_1306", "Hidden Ruins Guardian")]
    [InlineData("QUEST_1307", "Hidden Ruins Completion")]
    
    [InlineData("QUEST_1401", "Ruins Expansion Intro")]
    [InlineData("QUEST_1402", "Ruins Expansion Mapping")]
    [InlineData("QUEST_1403", "Ruins Expansion Threat")]
    [InlineData("QUEST_1404", "Ruins Expansion Return")]
    
    [InlineData("QUEST_2401", "Hidden Cove Discovery")]
    public void Phase3Arcs_HaveCompleteLocalization(string baseKey, string description)
    {
        // Each quest typically needs a Title, Desc, and Objective key
        var requiredKeys = new[]
        {
            $"{baseKey}_TITLE",
            $"{baseKey}_DESC",
            $"{baseKey}_OBJ"
        };

        foreach (var key in requiredKeys)
        {
            Assert.True(_baseKeys.Contains(key), $"Missed {key} in Strings.resx for {description}");
            Assert.True(_enKeys.Contains(key), $"Missed {key} in Strings.en.resx for {description}");
        }
    }

    private string FindSolutionRoot()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "TheWonderlandSolution.sln")))
        {
            current = Directory.GetParent(current)?.FullName;
        }

        if (current == null && File.Exists("/app/TheWonderlandSolution.sln"))
        {
            return "/app";
        }

        return current ?? throw new DirectoryNotFoundException("Could not find solution root.");
    }
}
