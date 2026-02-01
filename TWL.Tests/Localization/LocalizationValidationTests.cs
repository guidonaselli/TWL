using TWL.Tests.Localization.Audit;
using TWL.Tests.Localization.Audit.Reporters;

namespace TWL.Tests.Localization;

public class LocalizationValidationTests
{
    [Fact]
    public void RunLocalizationValidation()
    {
        // Setup paths
        var solutionRoot = FindSolutionRoot();
        var clientPath = Path.Combine(solutionRoot, "TWL.Client");
        var serverPath = Path.Combine(solutionRoot, "TWL.Server");
        var artifactsPath = Path.Combine(solutionRoot, "artifacts");

        // Ensure artifacts directory exists
        Directory.CreateDirectory(artifactsPath);

        var auditor = new LocalizationAuditor(solutionRoot, clientPath, serverPath);
        var results = auditor.RunAudit();

        var reporter = new JsonReporter(artifactsPath);
        reporter.GenerateReports(results);

        // Fail if there are ERRORS
        var errorCount = 0;
        Console.WriteLine($"Localization Audit Findings ({results.Findings.Count}):");
        foreach (var finding in results.Findings)
        {
            Console.WriteLine(
                $"[{finding.Severity}] {finding.Code}: {finding.Message} ({finding.File}:{finding.Location})");
            if (finding.Severity == "ERROR")
            {
                errorCount++;
            }
        }

        Assert.Equal(0, errorCount);
    }

    private string FindSolutionRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null && !File.Exists(Path.Combine(current, "TheWonderlandSolution.sln")))
        {
            current = Directory.GetParent(current)?.FullName;
        }

        return current ?? throw new DirectoryNotFoundException("Could not find solution root.");
    }
}