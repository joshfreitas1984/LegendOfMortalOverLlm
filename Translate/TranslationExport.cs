using FanslationStudio.LlmKit.Support;
using FanslationStudio.LlmKit.Workflow;

namespace Translate;

/// <summary>
/// Exports dumped per-category CSV assets into the custom translation format ahead of LLM
/// translation. Mirrors DragonHierOverLlm's Tests/TranslationExport.cs.
/// </summary>
public static class TranslationExport
{
    public static void ExportGameSpecificTextAssetsToCustomFormat(string workingDirectory)
    {
        foreach (var textFile in TextFileConfiguration.TextFilesToSplit.Where(t => t.TextFileType == TextFileType.RawCsv))
            // rawSubfolder differs from LlmKit's own default ("Raw/Dumped/GameData") - this repo's
            // BepInEx plugin dumps straight into Raw/Dumped/{file}, with no "GameData" subfolder.
            CsvGameDataWorkflow.ExportToCustomFormat(workingDirectory, textFile, GameFileHandling.SplitterOptions, rawSubfolder: "Raw/Dumped");
    }
}
