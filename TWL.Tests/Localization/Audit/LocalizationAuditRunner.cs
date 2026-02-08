using System.Text.Json;
using TWL.Tests.Localization.Audit;
using Xunit;
using Xunit.Abstractions;

namespace TWL.Tests.Localization.Audit;

public class LocalizationAuditRunner
{
    private readonly ITestOutputHelper _output;

    public LocalizationAuditRunner(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RunFullAudit()
    {
        // 1. Locate Solution Root
        // We assume we are in TWL.Tests/bin/Debug/net10.0/ or similar
        // Adjust as needed for the environment
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var solutionRoot = FindSolutionRoot(currentDir);

        Assert.NotNull(solutionRoot);
        _output.WriteLine($"Solution Root: {solutionRoot}");

        var clientPath = Path.Combine(solutionRoot, "TWL.Client");
        var serverPath = Path.Combine(solutionRoot, "TWL.Server");

        // 2. Run Auditor
        var auditor = new LocalizationAuditor(solutionRoot, clientPath, serverPath);
        var results = auditor.RunAudit();

        // 3. Generate Artifacts
        var artifactsDir = Path.Combine(solutionRoot, "artifacts");
        Directory.CreateDirectory(artifactsDir);

        var options = new JsonSerializerOptions { WriteIndented = true };

        // Index
        var index = new
        {
            results.ResourceKeys,
            results.UsedKeys,
            HardcodedCandidates = results.HardcodedStrings
        };
        File.WriteAllText(Path.Combine(artifactsDir, "localization-index.json"),
            JsonSerializer.Serialize(index, options));

        // Report
        // We only want findings in the report file usually, but the prompt says:
        // Code, Severity, File, LineOrPointer, Message, SuggestedFix
        File.WriteAllText(Path.Combine(artifactsDir, "localization-report.json"),
            JsonSerializer.Serialize(results.Findings, options));

        _output.WriteLine($"Artifacts generated in {artifactsDir}");

        // 4. Fail on ERROR
        var errors = results.Findings.Where(f => f.Severity == "ERROR").ToList();

        if (errors.Any())
        {
            _output.WriteLine("Localization Audit Errors:");
            foreach (var error in errors)
            {
                _output.WriteLine($"[{error.Code}] {error.File}: {error.Message} ({error.Location})");
            }

            // Fail if there are any ERROR findings.
            Assert.Fail($"Found {errors.Count} localization errors. See output or artifacts/localization-report.json for details.");
        }
    }

    private string? FindSolutionRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "TheWonderlandSolution.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        // Fallback for Docker environment if .sln is not found where expected or structure is different
        // In the provided file list, the .sln is at the root.
        if (Directory.Exists("/app"))
        {
            return "/app";
        }

        return null;
    }
}
