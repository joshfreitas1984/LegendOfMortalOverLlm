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
/// Patches <see cref="LeanLanguageCSV.Compile"/> rather than <see cref="LeanLanguageCSV.LoadFromSource"/>
/// - LoadFromSource is only called when a source's Entries list starts out empty (see
/// LeanLanguageCSV.Compile's decompiled body: "if ((entries == null || entries.Count == 0) &&
/// Application.isPlaying) LoadFromSource();"), and in practice almost every source in this game
/// already has its Entries pre-baked directly into the scene by Unity's own serializer at build
/// time, so LoadFromSource never fires for them at all (confirmed live: patching LoadFromSource
/// only ever dumped one source, "UpgradeItem_zh-tw", out of ~60). Compile(primaryLanguage,
/// defaultLanguage), by contrast, is called for EVERY registered LeanSource on every
/// LeanLocalization.UpdateTranslations() pass (see LeanLocalization.RegisterAndBuild iterating
/// LeanSource.Instances) regardless of whether Entries came from a pre-baked scene or a fresh
/// LoadFromSource() call triggered inside Compile itself - by the time our postfix runs, Entries
/// is guaranteed to reflect whichever path populated it.
///
/// The game ships THREE separate LeanLanguageCSV instances per category - one each for
/// "ChineseSimplified", "ChineseTraditional" and "Korean" (confirmed live via
/// __instance.Language) - and for most categories the TextAsset backing each one is itself
/// suffixed per language (e.g. "BattleSkill_zh-cn"/"BattleSkill_zh-tw"/"BattleSkill_kr"), so
/// they'd naturally land in different files anyway. The Story_N chapter files are the
/// exception: all three language variants share the exact same TextAsset name (e.g. "Story_2"
/// for Korean AND ChineseTraditional AND ChineseSimplified alike), so without filtering by
/// Language they silently overwrote each other in the raw dump folder (confirmed live - the
/// same "Story_2.csv" path logged three different entry counts back to back). Filtering to a
/// single <see cref="TargetLanguage"/> up front fixes both problems at once: only the wanted
/// language's data is ever dumped, and every source name is guaranteed unique per Compile call.
/// </summary>
internal static class StringTableDumpPatches
{
    private static readonly string PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
    private static readonly string RawDir = Path.Combine(PluginDir, "raw");
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>
    /// The only Language value ever dumped - set from MainPlugin's config-bound value. Defaults
    /// to "ChineseSimplified" (the game's exact Language string for its "CN" text, confirmed
    /// live), which is what this repo translates from.
    /// </summary>
    public static string TargetLanguage = "ChineseSimplified";

    [HarmonyPatch(typeof(LeanLanguageCSV), nameof(LeanLanguageCSV.Compile), new[] { typeof(string), typeof(string) })]
    [HarmonyPostfix]
    private static void Compile_Postfix(LeanLanguageCSV __instance)
    {
        try
        {
            if (__instance.Language != TargetLanguage)
                return;

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
            MainPlugin.Logger?.LogError($"StringTableDumpPatches: Compile_Postfix failed for '{__instance?.name}': {ex}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var sanitized = new string(name.Select(c => InvalidFileNameChars.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized;
    }
}
