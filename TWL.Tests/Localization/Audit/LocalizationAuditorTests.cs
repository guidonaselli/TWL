using System.Text.Json;
using TWL.Tests.Localization.Audit;
using Xunit;

namespace TWL.Tests.Localization.Audit;

public class LocalizationAuditorTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _clientPath;
    private readonly string _serverPath;
    private readonly string _contentPath;
    private readonly string _resourcesPath;

    public LocalizationAuditorTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "TWL_Audit_Temp_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);

        _clientPath = Path.Combine(_tempRoot, "TWL.Client");
        _serverPath = Path.Combine(_tempRoot, "TWL.Server");
        _contentPath = Path.Combine(_tempRoot, "Content", "Data");
        _resourcesPath = Path.Combine(_clientPath, "Resources");

        Directory.CreateDirectory(_clientPath);
        Directory.CreateDirectory(_serverPath);
        Directory.CreateDirectory(_contentPath);
        Directory.CreateDirectory(_resourcesPath);

        // Create config
        var configDir = Path.Combine(_tempRoot, "config");
        Directory.CreateDirectory(configDir);
        var config = new LocalizationAuditor.AuditConfig
        {
            RequiredLanguages = new List<string> { "base" },
            UiScanRoots = new List<string> { "TWL.Client/UI" }
        };
        File.WriteAllText(Path.Combine(configDir, "localization-audit-allowlist.json"),
            JsonSerializer.Serialize(config));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, true);
        }
    }

    [Fact]
    public void RunAudit_DetectsMissingKeys_FromCode()
    {
        // Arrange
        // Create Resource file
        var resxContent = @"<?xml version='1.0' encoding='utf-8'?>
<root>
  <data name=""EXISTING_KEY"" xml:space=""preserve"">
    <value>Existing Value</value>
  </data>
</root>";
        File.WriteAllText(Path.Combine(_resourcesPath, "Strings.resx"), resxContent);

        // Create Code file using a missing key
        var codeDir = Path.Combine(_clientPath, "UI");
        Directory.CreateDirectory(codeDir);
        File.WriteAllText(Path.Combine(codeDir, "TestUi.cs"),
            @"public class TestUi { public void Show() { var t = Loc.T(""MISSING_KEY""); } }");

        // Act
        var auditor = new LocalizationAuditor(_tempRoot, _clientPath, _serverPath);
        var results = auditor.RunAudit();

        // Assert
        Assert.Contains(results.Findings, f => f.Code == "ERR_MISSING_KEY" && f.Location == "MISSING_KEY");
    }

    [Fact]
    public void RunAudit_DetectsOrphanKeys()
    {
        // Arrange
        var resxContent = @"<?xml version='1.0' encoding='utf-8'?>
<root>
  <data name=""UNUSED_KEY"" xml:space=""preserve"">
    <value>Value</value>
  </data>
</root>";
        File.WriteAllText(Path.Combine(_resourcesPath, "Strings.resx"), resxContent);

        // Act
        var auditor = new LocalizationAuditor(_tempRoot, _clientPath, _serverPath);
        var results = auditor.RunAudit();

        // Assert
        Assert.Contains(results.Findings, f => f.Code == "WARN_ORPHAN_KEY" && f.Location == "UNUSED_KEY");
    }
}
