using System.Text.Json;

namespace TWL.Tests.Localization.Audit.Reporters;

public class JsonReporter
{
    private readonly string _artifactsPath;

    public JsonReporter(string artifactsPath)
    {
        _artifactsPath = artifactsPath;
    }

    public void GenerateReports(AuditResults results)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };

        var indexData = new
        {
            results.ResourceKeys,
            results.UsedKeys,
            HardcodedCandidates = results.HardcodedStrings
        };

        WriteWithRetry(
            Path.Combine(_artifactsPath, "localization-index.json"),
            JsonSerializer.Serialize(indexData, options));

        WriteWithRetry(
            Path.Combine(_artifactsPath, "localization-report.json"),
            JsonSerializer.Serialize(results.Findings, options));
    }

    private void WriteWithRetry(string path, string content)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                File.WriteAllText(path, content);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(500);
            }
        }
    }
}