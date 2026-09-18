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
    /// Parses one already-clean CSV line (as produced by <see cref="BuildRow"/>/dumped by this
    /// plugin) back into fields - quote-aware, doubled "" unescaped to a literal quote. Used by
    /// <see cref="StringTableInjectionPatches"/> to read the translated Mods/&lt;lang&gt;/*.csv
    /// files back at runtime.
    /// </summary>
    public static string[] ParseRow(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
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

            if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
