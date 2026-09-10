using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using Mortal.Core;

namespace LegendOfMortalPlugin;

/// <summary>
/// Replaces the third-party FunctionalPlugin_Binarizer's GetString redirect (a Harmony prefix on
/// the same method, backed by its own naive/unescaped CsvParser reading a single merged
/// Mods/&lt;lang&gt;/StringTable.csv) so this repo's own release no longer depends on that plugin
/// being present to inject the English text. Loads every Mods/&lt;lang&gt;/*.csv file this repo
/// packages (one per LeanLanguageCSV source - see StringTableDumpPatches/Translate/GameTextFiles)
/// into a single lookup dictionary keyed by the original fully-qualified key (each
/// LeanLanguageCSV.Entry.Name already includes its category prefix, e.g. "Story/D_8_2_...", so
/// keys stay unique across files with no extra namespacing needed).
/// </summary>
internal static class StringTableInjectionPatches
{
    private const string Language = "English";

    private static Dictionary<string, string>? _translations;

    private static Dictionary<string, string> Translations => _translations ??= LoadTranslations();

    [HarmonyPatch(typeof(LeanLocalizationResolver), nameof(LeanLocalizationResolver.GetString))]
    [HarmonyPrefix]
    private static bool GetString_Prefix(string key, ref string __result)
    {
        try
        {
            if (!string.IsNullOrEmpty(key) && Translations.TryGetValue(key, out var translated))
            {
                __result = translated;
                return false;
            }
        }
        catch (Exception ex)
        {
            MainPlugin.Logger?.LogError($"StringTableInjectionPatches: GetString_Prefix failed for key '{key}': {ex}");
        }

        return true;
    }

    private static Dictionary<string, string> LoadTranslations()
    {
        var result = new Dictionary<string, string>();
        var modDir = Path.Combine(Paths.GameRootPath, "Mods", Language);

        if (!Directory.Exists(modDir))
        {
            MainPlugin.Logger?.LogWarning($"StringTableInjectionPatches: no Mods/{Language} folder found at '{modDir}' - GetString will fall through to the original (Chinese) text.");
            return result;
        }

        foreach (var file in Directory.GetFiles(modDir, "*.csv"))
        {
            var loaded = 0;
            foreach (var line in File.ReadAllLines(file))
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                var fields = CsvUtility.ParseRow(line);
                if (fields.Length < 2 || string.IsNullOrEmpty(fields[0]))
                    continue;

                // Restore the literal "\n" our own dumper/packager writes for an embedded real
                // newline (see CsvUtility.EscapeField) back to an actual newline, matching how
                // LeanLanguageCSV.LoadFromSource itself replaces its "\\n" token when loading the
                // original Chinese source (see Lean.Localization.LeanLanguageCSV.NewLine).
                result[fields[0]] = fields[1].Replace("\\n", "\n");
                loaded++;
            }

            MainPlugin.Logger?.LogInfo($"StringTableInjectionPatches: loaded {loaded} entries from '{Path.GetFileName(file)}'");
        }

        return result;
    }
}
