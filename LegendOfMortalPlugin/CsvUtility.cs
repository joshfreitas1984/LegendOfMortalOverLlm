using System;
using System.Collections.Generic;
using System.Text;

namespace LegendOfMortalPlugin;

/// <summary>
/// Quote-aware CSV read/write helpers shared by the dump (<see cref="StringTableDumpPatches"/>)
/// and injection (<see cref="StringTableInjectionPatches"/>) patches. Deliberately independent of
/// FanslationStudio.LlmKit.Utility.CompoundFieldSplitter - this plugin runs inside the game
/// process (netstandard2.1/BepInEx5), not the net9.0 translation pipeline, and the two format
/// conventions are already the same (RFC4180-style quoting, doubled "" for a literal quote), so
/// there's no reason to duplicate more than these two small methods.
/// </summary>
internal static class CsvUtility
{
    /// <summary>
    /// Formats a single value as one RFC4180 CSV field: quoted only when needed (comma/quote/real
    /// newline present), doubled embedded quotes, and any real newline replaced with a literal
    /// "\n" two-character sequence so the row never spans more than one physical line - this is
    /// what lets FanslationStudio.LlmKit.Workflow.CsvGameDataWorkflow.ExportToCustomFormat read
    /// the dump with a plain File.ReadAllLines, no pre-clean pass required.
    /// </summary>
    public static string EscapeField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var normalized = value.Replace("\r\n", "\\n").Replace("\r", "\\n").Replace("\n", "\\n");

        if (normalized.IndexOfAny(new[] { ',', '"' }) < 0)
            return normalized;

        return "\"" + normalized.Replace("\"", "\"\"") + "\"";
    }

    public static string BuildRow(params string?[] fields)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(EscapeField(fields[i]));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses a whole CSV file's contents into rows - quote-aware across physical lines, so a
    /// quoted field containing a real newline (as FanslationStudio.LlmKit's CSV packager writes for
    /// multi-line entries) stays one field instead of being cut off at the first line break. Real
    /// newlines inside fields are normalized to "\n"; blank lines are skipped.
    /// </summary>
    public static IEnumerable<string[]> ParseFile(string content)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < content.Length && content[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                continue;

            if (inQuotes)
            {
                current.Append(c == '\r' ? '\n' : c);
                continue;
            }

            if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            if (c == '\n' || c == '\r')
            {
                fields.Add(current.ToString());
                current.Clear();
                if (fields.Count > 1 || fields[0].Length > 0)
                    yield return fields.ToArray();
                fields.Clear();
                continue;
            }

            current.Append(c);
        }

        fields.Add(current.ToString());
        if (fields.Count > 1 || fields[0].Length > 0)
            yield return fields.ToArray();
    }
}
