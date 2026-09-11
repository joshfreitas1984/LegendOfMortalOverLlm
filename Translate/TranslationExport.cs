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
        {
            // Story_*.csv dumps contain literal "\n" line-break escapes mid-sentence (e.g.
            // "...中人。\n难得与老兄..."), which CompoundFieldSplitter otherwise treats as a
            // fragment boundary, splitting one dialogue line into disjointed fragments/templates.
            // Strip them from the dump before export so each row translates as one continuous line.
            if (textFile.Path.StartsWith("Story_"))
                RemoveLiteralNewlinesFromDump(workingDirectory, textFile.Path);

            // rawSubfolder differs from LlmKit's own default ("Raw/Dumped/GameData") - this repo's
            // BepInEx plugin dumps straight into Raw/Dumped/{file}, with no "GameData" subfolder.
            CsvGameDataWorkflow.ExportToCustomFormat(workingDirectory, textFile, GameFileHandling.SplitterOptions, rawSubfolder: "Raw/Dumped");
        }
    }

    private static void RemoveLiteralNewlinesFromDump(string workingDirectory, string path)
    {
        var dumpedPath = $"{workingDirectory}/Raw/Dumped/{path}";
        var content = File.ReadAllText(dumpedPath);
        var stripped = content.Replace("\\n", "");

        if (stripped != content)
            File.WriteAllText(dumpedPath, stripped);
    }
}
