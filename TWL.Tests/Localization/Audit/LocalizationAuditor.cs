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
        private AuditConfig _config;
        private Dictionary<string, string> _baseResourceValues = new();

        public LocalizationAuditor(string solutionRoot, string clientPath, string serverPath)
        {
            _solutionRoot = solutionRoot;
            _clientPath = clientPath;
            _serverPath = serverPath;
            LoadConfig();
        }

        private void LoadConfig()
        {
            var configPath = Path.Combine(_solutionRoot, "config", "localization-audit-allowlist.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    _config = JsonSerializer.Deserialize<AuditConfig>(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading config: {ex.Message}");
                }
            }

            // Defaults if load failed or file missing
            if (_config == null)
            {
                _config = new AuditConfig
                {
                    RequiredLanguages = new List<string> { "base", "en" }
                };
            }
        }

    public class AuditConfig
    {
        public List<string> AllowedHardcodedLiterals { get; set; } = new();
        public List<string> AllowedFolders { get; set; } = new();
        public List<string> RequiredLanguages { get; set; } = new();
        public List<string> UiScanRoots { get; set; } = new();
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
                        var value = data.Element("value")?.Value;
                        if (!string.IsNullOrEmpty(key))
                        {
                            keys.Add(key);
                            if (lang == "base" && value != null)
                            {
                                _baseResourceValues[key] = value;
                            }
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
            var dataDir = Path.Combine(_solutionRoot, "Content", "Data");
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

            // Regex to find Loc.T("...") or Loc.TF("..."
            // Groups: 1=Method (T or TF), 2=Key
            var regex = new Regex(@"Loc\.(T|TF)\s*\(\s*""([^""]+)""");

            foreach (var file in sourceFiles)
            {
                var content = File.ReadAllText(file);
                var matches = regex.Matches(content);
                foreach (Match match in matches)
                {
                    var method = match.Groups[1].Value;
                    var key = match.Groups[2].Value;

                    results.UsedKeys.FromCode.Add(key);

                    if (method == "TF")
                    {
                        // Estimate arg count
                        int startIndex = match.Index + match.Length;
                        int argCount = CountArgs(content, startIndex);

                        if (!results.UsedKeys.TfArgCounts.ContainsKey(key))
                        {
                            results.UsedKeys.TfArgCounts[key] = new List<int>();
                        }
                        results.UsedKeys.TfArgCounts[key].Add(argCount);
                    }
                }
            }
            results.UsedKeys.FromCode = results.UsedKeys.FromCode.Distinct().ToList();
        }

        private int CountArgs(string content, int startIndex)
        {
            int i = startIndex;
            bool insideString = false;

            // Check if immediate close or comma
            // Skip whitespace
            while(i < content.Length && char.IsWhiteSpace(content[i])) i++;

            if (i >= content.Length || content[i] == ')') return 0; // No extra args
            if (content[i] == ',')
            {
                i++; // Skip first comma
            }
            else
            {
                // Should be a comma if there are args
                return 0;
            }

            int args = 1;
            int parensLevel = 0;

            for (; i < content.Length; i++)
            {
                char c = content[i];

                if (c == '"' && (i == 0 || content[i-1] != '\\'))
                {
                    insideString = !insideString;
                    continue;
                }

                if (insideString) continue;

                if (c == '(') parensLevel++;
                else if (c == ')')
                {
                    if (parensLevel == 0) return args; // End of Loc.TF call
                    parensLevel--;
                }
                else if (c == ',' && parensLevel == 0)
                {
                    args++;
                }
            }
            return args;
        }

        private void ScanHardcodedStrings(AuditResults results)
        {
            var roots = _config.UiScanRoots.Count > 0
                ? _config.UiScanRoots
                : new List<string> { Path.Combine("TWL.Client", "Presentation", "UI") };

            var files = new List<string>();
            foreach(var root in roots)
            {
                 var fullPath = Path.Combine(_solutionRoot, root);
                 if(Directory.Exists(fullPath))
                    files.AddRange(Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories));
            }

            // Simple heuristic regex for finding strings
            var stringRegex = new Regex(@"""([^""]+)""");

            foreach (var file in files)
            {
                // Check allowed folders
                if (_config.AllowedFolders.Any(ignored => file.Contains(Path.DirectorySeparatorChar + ignored + Path.DirectorySeparatorChar))) continue;

                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith("//") || line.StartsWith("using ") || line.Contains("Log(") || line.Contains("Console.Write") || line.Contains("// loc: ignore"))
                        continue;

                    var matches = stringRegex.Matches(line);
                    foreach (Match match in matches)
                    {
                        var val = match.Groups[1].Value;
                        if (string.IsNullOrWhiteSpace(val)) continue;
                        if (val.Length < 2) continue; // Skip single chars usually
                        if (val.Contains(".json") || val.Contains(".png") || val.Contains("/")) continue; // likely paths

                        // Check allowlist literals
                        if (_config.AllowedHardcodedLiterals.Contains(val)) continue;

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
            foreach (var lang in _config.RequiredLanguages)
            {
                if (!results.ResourceKeys.ContainsKey(lang))
                {
                    results.Findings.Add(new AuditFinding
                    {
                        Code = $"ERR_NO_{lang.ToUpper()}_RESOURCES",
                        Severity = "ERROR",
                        File = lang == "base" ? "Resources/Strings.resx" : $"Resources/Strings.{lang}.resx",
                        Location = "Project Root",
                        Message = $"Resource file for required language '{lang}' is missing.",
                        SuggestedFix = $"Create Resources/Strings.{lang}.resx"
                    });
                    continue;
                }

                var langKeys = results.ResourceKeys[lang];
                foreach (var key in allUsedKeys)
                {
                    if (!langKeys.Contains(key))
                    {
                        results.Findings.Add(new AuditFinding
                        {
                            Code = "ERR_MISSING_KEY",
                            Severity = "ERROR",
                            File = lang == "base" ? "Resources/Strings.resx" : $"Resources/Strings.{lang}.resx",
                            Location = key,
                            Message = $"Key '{key}' is used but missing from '{lang}' resources.",
                            SuggestedFix = $"Add data name='{key}' to Strings.{lang}.resx"
                        });
                    }
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

            // 4. Key Naming Convention
            var validPrefixes = new[] { "UI_", "ERR_", "QUEST_", "SKILL_", "ITEM_", "TUTORIAL_" };
            foreach (var key in allUsedKeys)
            {
                 if (!validPrefixes.Any(p => key.StartsWith(p)))
                 {
                     results.Findings.Add(new AuditFinding
                     {
                         Code = "WARN_NAMING_CONVENTION",
                         Severity = "WARN",
                         File = "N/A",
                         Location = key,
                         Message = $"Key '{key}' does not follow naming convention (prefixes: {string.Join(", ", validPrefixes)})",
                         SuggestedFix = "Rename key with valid prefix."
                     });
                 }
            }

            // 5. Format Safety
            foreach (var kvp in results.UsedKeys.TfArgCounts)
            {
                var key = kvp.Key;
                var argCounts = kvp.Value;

                if (_baseResourceValues.TryGetValue(key, out var resourceValue))
                {
                    // Count unique placeholders like {0}, {1}, {0:0.00}
                    // Regex matches {n} or {n:format}
                    var matches = Regex.Matches(resourceValue, @"\{(\d+)(?::[^}]+)?\}");
                    var maxPlaceholderIndex = -1;
                    foreach(Match m in matches)
                    {
                        if (int.TryParse(m.Groups[1].Value, out int idx))
                        {
                            if (idx > maxPlaceholderIndex) maxPlaceholderIndex = idx;
                        }
                    }
                    var expectedArgs = maxPlaceholderIndex + 1; // e.g. {0} needs 1 arg.

                    foreach(var count in argCounts)
                    {
                        if (count != expectedArgs)
                        {
                            results.Findings.Add(new AuditFinding
                            {
                                Code = "ERR_FORMAT_MISMATCH",
                                Severity = "ERROR",
                                File = "Resources/Strings.resx",
                                Location = key,
                                Message = $"Format mismatch for '{key}': Resource has {expectedArgs} placeholders, code supplies {count} arguments.",
                                SuggestedFix = "Fix code arguments or resource string."
                            });
                        }
                    }
                }
            }
        }
    }
}
