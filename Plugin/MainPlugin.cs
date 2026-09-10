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

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(StringTableDumpPatches));
        harmony.PatchAll(typeof(StringTableInjectionPatches));

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded.");
    }
}
