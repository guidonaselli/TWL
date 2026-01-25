using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Text.Json;

namespace TWL.Tests.Localization.Audit
{
    public class LocalizationAuditor
    {
        private readonly string _solutionRoot;
        private readonly string _clientPath;
        private readonly string _serverPath;
        private readonly List<string> _requiredLanguages = new() { "base", "en" }; // Default required

        public LocalizationAuditor(string solutionRoot, string clientPath, string serverPath)
        {
            _solutionRoot = solutionRoot;
            _clientPath = clientPath;
            _serverPath = serverPath;
        }

        public AuditResults RunAudit()
        {
            var results = new AuditResults();

            ScanResources(results);
            ScanContent(results);
            ScanCode(results);
            ScanHardcodedStrings(results);
            AnalyzeFindings(results);

            return results;
        }

        private void ScanResources(AuditResults results)
        {
            var resourceDir = Path.Combine(_clientPath, "Resources");
            if (!Directory.Exists(resourceDir))
            {
                // No resources directory found
                return;
            }

            var files = Directory.GetFiles(resourceDir, "*.resx");
            foreach (var file in files)
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                var lang = filename.Replace("Strings", "").Replace(".", "");
                if (string.IsNullOrEmpty(lang)) lang = "base";

                var keys = new List<string>();
                try
                {
                    var doc = XDocument.Load(file);
                    foreach (var data in doc.Descendants("data"))
                    {
                        var key = data.Attribute("name")?.Value;
                        if (!string.IsNullOrEmpty(key))
                        {
                            keys.Add(key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading {file}: {ex.Message}");
                }

                results.ResourceKeys[lang] = keys;
            }
        }

        private void ScanContent(AuditResults results)
        {
            var dataDir = Path.Combine(_serverPath, "Content", "Data");
            if (!Directory.Exists(dataDir)) return;

            var files = Directory.GetFiles(dataDir, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);
                    ScanJsonElement(doc.RootElement, results.UsedKeys.FromContent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing {file}: {ex.Message}");
                }
            }
            // Remove duplicates
            results.UsedKeys.FromContent = results.UsedKeys.FromContent.Distinct().ToList();
        }

        private void ScanJsonElement(JsonElement element, List<string> keys)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.EndsWith("Key") && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var val = property.Value.GetString();
                        if (!string.IsNullOrEmpty(val))
                        {
                            keys.Add(val);
                        }
                    }
                    ScanJsonElement(property.Value, keys);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    ScanJsonElement(item, keys);
                }
            }
        }

        private void ScanCode(AuditResults results)
        {
            var sourceFiles = Directory.EnumerateFiles(_solutionRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains("Tests"));

            var regex = new Regex(@"Loc\.T(?:F)?\(\s*""([^""]+)""");

            foreach (var file in sourceFiles)
            {
                var content = File.ReadAllText(file);
                var matches = regex.Matches(content);
                foreach (Match match in matches)
                {
                    results.UsedKeys.FromCode.Add(match.Groups[1].Value);
                }
            }
            results.UsedKeys.FromCode = results.UsedKeys.FromCode.Distinct().ToList();
        }

        private void ScanHardcodedStrings(AuditResults results)
        {
            var uiDir = Path.Combine(_clientPath, "Presentation", "UI");
            if (!Directory.Exists(uiDir)) return;

            var files = Directory.GetFiles(uiDir, "*.cs", SearchOption.AllDirectories);
            // Simple heuristic regex for finding strings
            var stringRegex = new Regex(@"""([^""]+)""");

            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith("//") || line.StartsWith("using ") || line.Contains("Log(") || line.Contains("Console.Write"))
                        continue;

                    var matches = stringRegex.Matches(line);
                    foreach (Match match in matches)
                    {
                        var val = match.Groups[1].Value;
                        if (string.IsNullOrWhiteSpace(val)) continue;
                        if (val.Length < 2) continue; // Skip single chars usually
                        if (val.Contains(".json") || val.Contains(".png") || val.Contains("/")) continue; // likely paths

                        // Ignore if it's wrapped in Loc.T(...) or Loc.TF(...)
                        // This is a simple heuristic and might miss complex cases, but reduces noise.
                        var escapedVal = Regex.Escape(val);
                        if (Regex.IsMatch(line, $@"Loc\.TF?\(\s*""{escapedVal}""")) continue;

                        // Heuristic: user facing strings usually contain spaces or start with capital letters
                        // But menu items might be "Login"

                        results.HardcodedStrings.Add(new HardcodedString
                        {
                            File = Path.GetRelativePath(_solutionRoot, file),
                            Line = i + 1,
                            Snippet = line,
                            Reason = "Potential UI string"
                        });
                    }
                }
            }
        }

        private void AnalyzeFindings(AuditResults results)
        {
            var allUsedKeys = results.UsedKeys.FromContent.Concat(results.UsedKeys.FromCode).Distinct().ToList();
            var baseKeys = results.ResourceKeys.ContainsKey("base") ? results.ResourceKeys["base"] : new List<string>();

            // 1. Missing Keys
            if (!results.ResourceKeys.ContainsKey("base"))
            {
                results.Findings.Add(new AuditFinding
                {
                    Code = "ERR_NO_BASE_RESOURCES",
                    Severity = "ERROR",
                    File = "Resources/Strings.resx",
                    Location = "Project Root",
                    Message = "Base resource file is missing.",
                    SuggestedFix = "Create Resources/Strings.resx"
                });
            }

            foreach (var key in allUsedKeys)
            {
                if (!baseKeys.Contains(key))
                {
                    results.Findings.Add(new AuditFinding
                    {
                        Code = "ERR_MISSING_KEY",
                        Severity = "ERROR",
                        File = "Resources/Strings.resx",
                        Location = key,
                        Message = $"Key '{key}' is used but missing from base resources.",
                        SuggestedFix = $"Add data name='{key}' to Strings.resx"
                    });
                }
            }

            // 2. Hardcoded Strings
            foreach (var hardcoded in results.HardcodedStrings)
            {
                results.Findings.Add(new AuditFinding
                {
                    Code = "WARN_HARDCODED_UI",
                    Severity = "WARN",
                    File = hardcoded.File,
                    Location = hardcoded.Line.ToString(),
                    Message = $"Potential hardcoded string found: {hardcoded.Snippet}",
                    SuggestedFix = "Use Loc.T()"
                });
            }

            // 3. Orphan Keys
             foreach (var key in baseKeys)
             {
                 if (!allUsedKeys.Contains(key))
                 {
                     results.Findings.Add(new AuditFinding
                     {
                         Code = "WARN_ORPHAN_KEY",
                         Severity = "WARN",
                         File = "Resources/Strings.resx",
                         Location = key,
                         Message = $"Key '{key}' is present in resources but not used.",
                         SuggestedFix = "Remove if unused."
                     });
                 }
             }
        }
    }
}
