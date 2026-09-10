using System.Text;
using Translate.Support;
using Translate.Utility;
using ToolGood.Words;

namespace Translate.Tests;

public class GlossaryTests
{
    const string workingDirectory = "../../../../Files";

    [Fact]
    public void UpdateSimplifiedAndTraditional()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        foreach (var line in config.GlossaryLines)
        {
            if (string.IsNullOrEmpty(line.RawSimplified) && string.IsNullOrEmpty(line.RawTraditional))
            {
                var traditional = WordsHelper.ToTraditionalChinese(line.Raw);
                var simplified = WordsHelper.ToSimplifiedChinese(line.Raw);

                if (simplified != line.Raw)
                {
                    line.RawTraditional = line.Raw;
                    line.RawSimplified = simplified;
                }
                else if (traditional != line.Raw)
                {
                    line.RawSimplified = line.Raw;
                    line.RawTraditional = traditional;
                }
            }
        }
        var serializer = Yaml.CreateSerializer();
        var yaml = serializer.Serialize(config.GlossaryLines);
        File.WriteAllText($"{workingDirectory}/TestResults/Glossary/UpdatedGlossary.yaml", yaml);
    }

    [Fact]
    public void CheckForDuplicateGlossaryItems()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Check for duplicate `raw` entries in config.GlossaryLines and config.ManualTranslations
        var cache = new Dictionary<string, string>();
        var duplicates = new List<string>();
        var allEntries = config.GlossaryLines.Concat(config.ManualTranslations);

        foreach (var entry in allEntries)
        {
            if (cache.ContainsKey(entry.Raw))
            {
                if (!duplicates.Contains(entry.Raw))
                    duplicates.Add(entry.Raw);
            }
            else if(cache.ContainsKey(entry.RawSimplified))
            { 
                if (!duplicates.Contains(entry.RawSimplified))
                    duplicates.Add(entry.RawSimplified);
            }
            else if (cache.ContainsKey(entry.RawTraditional))
            {
                if (!duplicates.Contains(entry.RawTraditional))
                    duplicates.Add(entry.RawTraditional);
            }
            else
            {
                cache.Add(entry.Raw, entry.Result);
            }
        }

        Directory.CreateDirectory($"{workingDirectory}/TestResults");
        File.WriteAllLines($"{workingDirectory}/TestResults/Glossary/DupeGlossary.yaml", duplicates);

        // Check for entries in `raw` with similar `raw` but different `result` in config.GlossaryLines
        var similarEntries = new List<string>();
        var glossaryList = config.GlossaryLines.ToList();

        for (int i = 0; i < glossaryList.Count; i++)
        {
            for (int j = i + 1; j < glossaryList.Count; j++)
            {
                var entry1 = glossaryList[i];
                var entry2 = glossaryList[j];

                // Check if raw values are similar but results are different
                if (AreSimilar(entry1.Raw, entry2.Raw) && entry1.Result != entry2.Result)
                {
                    var similarPair = $"- raw1: \"{entry1.Raw}\" result1: \"{entry1.Result}\"" +
                                    $"\n  raw2: \"{entry2.Raw}\" result2: \"{entry2.Result}\"";
                    if (!similarEntries.Contains(similarPair))
                        similarEntries.Add(similarPair);
                }
            }
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/Glossary/SimilarGlossary.yaml", similarEntries);
    }

    private static bool AreSimilar(string str1, string str2)
    {
        if (str1 == str2)
            return false; // Exact duplicates are not "similar", they're identical

        // Check case-insensitive equality
        if (string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check trimmed equality
        if (str1.Trim() == str2.Trim())
            return true;

        // Check Levenshtein distance for close matches
        var distance = LevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);
        return maxLength > 0 && (double)distance / maxLength <= 0.2; // 20% difference threshold
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1))
            return string.IsNullOrEmpty(s2) ? 0 : s2.Length;
        if (string.IsNullOrEmpty(s2))
            return s1.Length;

        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(
                    matrix[i - 1, j] + 1,      // deletion
                    matrix[i, j - 1] + 1),     // insertion
                    matrix[i - 1, j - 1] + cost); // substitution
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    [Fact]
    public void CleanupManualTranslations_RemovesEmptyEntries()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var cache = new Dictionary<string, string>();
        var badDupes = new List<string>();
        var cleanedManuals = new List<GlossaryLine>();
        var cleanMe = new List<string>();

        foreach (var k in config.ManualTranslations)
        {
            if (cache.ContainsKey(k.Raw))
            {
                if (k.Result == cache[k.Raw])
                    cleanMe.Add(k.Raw);
                else
                    badDupes.Add(k.Raw);
            }
            else
            {
                cache.Add(k.Raw, k.Result);
                cleanedManuals.Add(k);
            }
        }

        if (badDupes.Count > 0)
        {
            File.WriteAllLines($"{workingDirectory}/TestResults/Glossary/DupeBadGlossary.yaml", badDupes);
            throw new Exception($"Duplicate manual translations with different results found");
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/Glossary/DupeGlossary.yaml", cleanMe);

        //var serializer = Yaml.CreateSerializer();
        //var clean = serializer.Serialize(cleanedManuals);
        //File.WriteAllText($"{workingDirectory}/TestResults/CleanManualTranslations.yaml", clean);
    }

    [Fact]
    public void CheckForConflictingGlossaryItems()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var allEntries = config.GlossaryLines.Concat(config.ManualTranslations).ToList();
        var conflicts = new List<string>();

        // Check if raw of one entry contains raw, rawSimplified, or rawTraditional of another entry
        for (int i = 0; i < allEntries.Count; i++)
        {
            var entry1 = allEntries[i];

            for (int j = 0; j < allEntries.Count; j++)
            {
                if (i == j) continue;

                var entry2 = allEntries[j];

                // Get all variants of entry2 to check if they're contained in entry1's raw
                var entry2Variants = new List<string> { entry2.Raw };
                if (!string.IsNullOrEmpty(entry2.RawSimplified))
                    entry2Variants.Add(entry2.RawSimplified);
                if (!string.IsNullOrEmpty(entry2.RawTraditional))
                    entry2Variants.Add(entry2.RawTraditional);

                // Check if entry1's raw contains any variant of entry2
                bool containsVariant = false;
                string containedVariant = "";
                foreach (var variant in entry2Variants)
                {
                    if (entry1.Raw != variant && entry1.Raw.Contains(variant))
                    {
                        containsVariant = true;
                        containedVariant = variant;
                        break;
                    }
                }

                if (containsVariant)
                {
                    // Only check for conflicts if the contained entry has badtrans = true
                    if (!entry2.CheckForBadTranslation)
                        continue;

                    // Check if entry1 has the result or allowed alternatives from entry2
                    bool hasEntry2Result = entry1.Result.Contains(entry2.Result, StringComparison.OrdinalIgnoreCase);
                    bool hasEntry2InAlts = entry1.AllowedAlternatives.Any(alt =>
                        alt.Contains(entry2.Result, StringComparison.OrdinalIgnoreCase));
                    bool entry2HasEntry1InAlts = entry2.AllowedAlternatives.Any(alt =>
                        alt.Contains(entry1.Result, StringComparison.OrdinalIgnoreCase));
                    bool entry1HasEntry2Alts = entry2.AllowedAlternatives.Any(alt =>
                        entry1.Result.Contains(alt, StringComparison.OrdinalIgnoreCase));

                    if (!hasEntry2Result && !hasEntry2InAlts && !entry2HasEntry1InAlts && !entry1HasEntry2Alts)
                    {
                        var conflict = new StringBuilder();
                        conflict.AppendLine($"Containment conflict found:");
                        conflict.AppendLine($"  Containing Entry (has '{containedVariant}' in '{entry1.Raw}'):");
                        conflict.AppendLine($"    raw: \"{entry1.Raw}\"");
                        if (!string.IsNullOrEmpty(entry1.RawSimplified))
                            conflict.AppendLine($"    rawSimplified: \"{entry1.RawSimplified}\"");
                        if (!string.IsNullOrEmpty(entry1.RawTraditional))
                            conflict.AppendLine($"    rawTraditional: \"{entry1.RawTraditional}\"");
                        conflict.AppendLine($"    result: \"{entry1.Result}\"");
                        if (entry1.AllowedAlternatives.Count > 0)
                            conflict.AppendLine($"    allowedAlternatives: [{string.Join(", ", entry1.AllowedAlternatives.Select(a => $"\"{a}\""))}]");

                        conflict.AppendLine($"  Contained Entry (badtrans = true):");
                        conflict.AppendLine($"    raw: \"{entry2.Raw}\"");
                        if (!string.IsNullOrEmpty(entry2.RawSimplified))
                            conflict.AppendLine($"    rawSimplified: \"{entry2.RawSimplified}\"");
                        if (!string.IsNullOrEmpty(entry2.RawTraditional))
                            conflict.AppendLine($"    rawTraditional: \"{entry2.RawTraditional}\"");
                        conflict.AppendLine($"    result: \"{entry2.Result}\"");
                        if (entry2.AllowedAlternatives.Count > 0)
                            conflict.AppendLine($"    allowedAlternatives: [{string.Join(", ", entry2.AllowedAlternatives.Select(a => $"\"{a}\""))}]");

                        conflicts.Add(conflict.ToString());
                    }
                }
            }
        }

        Directory.CreateDirectory($"{workingDirectory}/TestResults/Glossary/");
        File.WriteAllLines($"{workingDirectory}/TestResults/Glossary/ConflictingGlossary.yaml", conflicts);
    }
}
