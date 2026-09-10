using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Lean.Localization;

namespace LegendOfMortalPlugin;

/// <summary>
/// Dumps the game's own per-file localization sources to clean, quote-aware CSV - one file per
/// <see cref="LeanLanguageCSV"/> component, named after its backing TextAsset (e.g. Story.csv,
/// LegendInfo.csv, CombatSkill.csv), instead of the single merged/naively-quoted StringTable.csv
/// the third-party FunctionalPlugin_Binarizer's F4 ExportLocalizations() hotkey produces (see
/// investigation notes: that export is "key,\"value\"" with no escaping of embedded quotes,
/// commas, or newlines in value - the "known incomplete ad-hoc method" this replaces).
///
/// Every LeanLanguageCSV in the scene calls LoadFromSource() once its own entries are needed
/// (see LeanLocalization.dll's decompiled Compile()/LoadFromSource()), so patching that method
/// directly - rather than reading the merged LeanLocalization.CurrentTranslations dictionary -
/// gives each source's own Name/Text pairs without having to re-derive file boundaries from key
/// prefixes ourselves.
/// </summary>
internal static class StringTableDumpPatches
{
    private static readonly string PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
    private static readonly string RawDir = Path.Combine(PluginDir, "raw");
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    [HarmonyPatch(typeof(LeanLanguageCSV), nameof(LeanLanguageCSV.LoadFromSource))]
    [HarmonyPostfix]
    private static void LoadFromSource_Postfix(LeanLanguageCSV __instance)
    {
        try
        {
            var entries = __instance.Entries;
            if (entries == null || entries.Count == 0)
                return;

            var sourceName = __instance.Source != null && !string.IsNullOrEmpty(__instance.Source.name)
                ? __instance.Source.name
                : __instance.name;

            var fileName = SanitizeFileName(sourceName);

            Directory.CreateDirectory(RawDir);

            var sb = new StringBuilder();
            foreach (var entry in entries)
                sb.Append(CsvUtility.BuildRow(entry.Name, entry.Text)).Append('\n');

            File.WriteAllText(Path.Combine(RawDir, $"{fileName}.csv"), sb.ToString(), new UTF8Encoding(false));

            MainPlugin.Logger?.LogInfo($"StringTableDumpPatches: dumped {entries.Count} entries from '{sourceName}' ({__instance.Language}) to {fileName}.csv");
        }
        catch (Exception ex)
        {
            MainPlugin.Logger?.LogError($"StringTableDumpPatches: LoadFromSource_Postfix failed for '{__instance?.name}': {ex}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var sanitized = new string(name.Select(c => InvalidFileNameChars.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized;
    }
}
