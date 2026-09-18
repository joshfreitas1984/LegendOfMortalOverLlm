using FanslationStudio.LlmKit.Support;
using FanslationStudio.LlmKit.Workflow;

namespace Translate;

/// <summary>
/// Packages translated per-category CSV files into the final Mod/ output. Mirrors
/// DragonHierOverLlm's Tests/TranslationPackaging.cs.
///
/// Unlike the OLD Translate/FileOutputHandling.cs::PackageFinalTranslationAsync, this never merges
/// files together - CsvGameDataWorkflow.PackageAsync already writes one Mod/{file} per
/// TextFileToSplit, which is what we want (see task background: dumps/packages are per-file now,
/// never merged the way the old pipeline merged everything into one Mod/English/StringTable.csv).
///
/// No LegendInfo-specific paragraph-newline post-processing is needed here (the old pipeline had
/// one - see git history around FileOutputHandling.cs for reference). Confirmed live against the
/// real Legend_01_zh-cn.csv dump: every line break is a single, uniform literal "\n" (never the
/// old pipeline's doubled "\\n\\n" artifact, which was itself a byproduct of that pipeline's own
/// ad-hoc pre-clean regex, not something the game's data actually contains). A literal "\n" is
/// just ordinary non-Chinese text to CompoundFieldSplitter.Decompose/Reconstruct - it's preserved
/// verbatim in the template and survives translation unchanged with no special-casing required.
/// </summary>
public static class TranslationPackaging
{
    public static async Task PackageFinalTranslationAsync(string workingDirectory, TextFileToSplit[] textFiles)
    {
        string outputPath = $"{workingDirectory}/Mod";

        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true);

        Directory.CreateDirectory(outputPath);

        var passedCount = 0;
        var qcRejectedCount = 0;
        var rawFallbackCount = 0;

        foreach (var textFile in textFiles.Where(t => t.TextFileType == TextFileType.RawCsv))
        {
            var (passed, qcRejected, rawFallback) = await CsvGameDataWorkflow.PackageAsync(workingDirectory, textFile);

            passedCount += passed;
            qcRejectedCount += qcRejected;
            rawFallbackCount += rawFallback;
        }

        Console.WriteLine($"Passed: {passedCount}");
        Console.WriteLine($"QC failures: {qcRejectedCount}");
        Console.WriteLine($"Fell back to raw: {rawFallbackCount}");
    }
}
