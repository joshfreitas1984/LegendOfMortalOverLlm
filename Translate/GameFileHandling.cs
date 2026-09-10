using FanslationStudio.LlmKit.Configuration;
using FanslationStudio.LlmKit.Utility;

namespace Translate;

/// <summary>
/// Game-specific constants and LLM repair/validation hooks for Legend of Mortal's translation
/// pipeline, mirroring DragonHierOverLlm's Tests/GameFileHandling.cs. The rest of the pipeline is
/// split across TextFileConfiguration (per-file config), TranslationExport (dump -> custom format)
/// and TranslationPackaging (custom format -> Mod/*.csv).
/// </summary>
public static class GameFileHandling
{
    public static readonly string WorkingDirectory = "../../../../Files";
    public static readonly string GameFolder = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\LegendOfMortal";

    /// <summary>
    /// Options for <see cref="Utility.CompoundFieldSplitter"/>. Sampled Files/Raw/StringTable.csv's
    /// text column directly (grepping for "#\w+#"/"\{\d+\}"-shaped tokens) before writing this: the
    /// only placeholder syntax found anywhere in this game's dumped text is plain positional
    /// "{0}".."{4}" (52/16/12/7/2 occurrences respectively, plus one stray literal "{title}") - no
    /// "#PlayerName#"-style token exists anywhere in the data, unlike DragonHeir's game. Those
    /// "{n}" tokens are already handled generically and safely by CompoundFieldSplitter itself
    /// (it escapes any literal '{'/'}' in a cell to '⟦'/'⟧' before generating its own fragment
    /// placeholders, then restores them with correct word-boundary spacing on reconstruct - see
    /// CompoundFieldSplitter.Decompose/Reconstruct), so no PlaceholderPatterns entry is needed for
    /// them either - DragonHeir's own GameFileHandling.cs deliberately leaves an equivalent
    /// "\{\d+\}" pattern commented out for the same reason. Left as an empty/default instance;
    /// revisit if a future dump reveals a real custom placeholder token.
    /// </summary>
    internal static readonly CompoundFieldSplitterOptions SplitterOptions = new();

    /// <summary>
    /// Game-specific translation repair/validation hooks - see
    /// <see cref="FanslationStudio.LlmKit.Configuration.GameHooks"/>. Left empty/pass-through: the
    /// OLD Translate/LineValidation.cs's repair/validation logic (CleanupLineBeforeSaving's
    /// character normalization, CheckTransalationSuccessful's placeholder/tag/punctuation checks,
    /// etc.) is now entirely handled generically inside FanslationStudio.LlmKit's own
    /// LineValidation - nothing in the old file looked specific to Legend of Mortal's own game
    /// syntax the way DragonHeir's "#TargetInteractName#" placeholder-repair or PlotData column-9
    /// delimiter validation are. NEEDS HUMAN REVIEW: flag if a future translation run turns up a
    /// Legend-of-Mortal-specific LLM quirk (e.g. something tied to the "{0}".."{4}" placeholders,
    /// or the ⑩/⓪/①-⑨ numbered-list glyphs) that would benefit from a CustomPostRepair/
    /// CustomColumnRepair/CustomColumnValidator hook here.
    /// </summary>
    public static readonly GameHooks Hooks = new();
}
