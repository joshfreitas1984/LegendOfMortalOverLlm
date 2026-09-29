using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace LegendOfMortalPlugin;

/// <summary>
/// Dumps the game's own LeanLocalization CSV sources to clean, per-file CSV (StringTableDumpPatches)
/// and redirects LeanLocalizationResolver.GetString to this repo's own packaged English translation
/// (StringTableInjectionPatches), replacing the third-party FunctionalPlugin_Binarizer for both
/// jobs.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MainPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger = null!;

    private void Awake()
    {
        Logger = base.Logger;

        var dumpEnabled = Config.Bind(
            "Dumper", "Enabled", false,
            "Whether to dump the game's LeanLocalization CSV sources to the plugin's raw folder. Off by default - only needed when refreshing Files/Raw/Dumped from a new game build.").Value;

        StringTableDumpPatches.TargetLanguage = Config.Bind(
            "General", "DumpLanguage", "ChineseSimplified",
            "The LeanLocalization Language value to dump - the game ships ChineseSimplified/ChineseTraditional/Korean variants of most files, and several of them (e.g. every Story_N chapter) all share the same TextAsset name across languages, so only one Language is ever dumped at a time to avoid them overwriting each other.").Value;

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        if (dumpEnabled)
            harmony.PatchAll(typeof(StringTableDumpPatches));
        harmony.PatchAll(typeof(StringTableInjectionPatches));

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded.");
    }
}
