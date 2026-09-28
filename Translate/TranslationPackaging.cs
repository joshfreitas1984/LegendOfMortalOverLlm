using FanslationStudio.LlmKit;
using FanslationStudio.LlmKit.Utility;
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
/// The one exception is <see cref="SoftLineBreakJoiner"/>, which joins the source's mid-sentence
/// manual wraps into spaces for every RawCsv file (see <see cref="PackageCsvAsync"/>).
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

        foreach (var textFile in textFiles)
        {
            var (passed, qcRejected, rawFallback) = textFile.TextFileType switch
            {
                TextFileType.RawCsv => await PackageCsvAsync(workingDirectory, textFile),
                TextFileType.PrefabText => await PrefabTextWorkflow.PackagePrefabTextAsync(workingDirectory, textFile),
                // Mono game: DynamicStrings is the Cecil-transpiler flavour, not DynamicStringsIL2CPP.
                TextFileType.DynamicStrings => await DynamicStringsCecilWorkflow.PackageDynamicStringsCecilAsync(workingDirectory, textFile),
                _ => (0, 0, 0),
            };

            passedCount += passed;
            qcRejectedCount += qcRejected;
            rawFallbackCount += rawFallback;
        }

        Console.WriteLine($"Passed: {passedCount}");
        Console.WriteLine($"QC failures: {qcRejectedCount}");
        Console.WriteLine($"Fell back to raw: {rawFallbackCount}");
    }

    /// <summary>
    /// CsvGameDataWorkflow.PackageAsync plus <see cref="SoftLineBreakJoiner"/> as its row
    /// post-process. The hook only sees the packaged row, so the original Chinese row is looked up
    /// by its key (column 0) from the same Converted file.
    /// </summary>
    private static async Task<(int Passed, int QcRejected, int RawFallback)> PackageCsvAsync(string workingDirectory, TextFileToSplit textFile)
    {
        if (!SoftLineBreakJoiner.Enabled)
            return await CsvGameDataWorkflow.PackageAsync(workingDirectory, textFile);

        var sourceRowsByKey = new Dictionary<string, string[]>();
        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, [textFile], (_, _, lines) =>
        {
            foreach (var line in lines)
            {
                var fields = CompoundFieldSplitter.ParseCsvRow(line.Raw);
                if (fields.Length > 0)
                    sourceRowsByKey.TryAdd(fields[0], fields);
            }

            return Task.CompletedTask;
        });

        return await CsvGameDataWorkflow.PackageAsync(workingDirectory, textFile,
            rowPostProcess: fields => fields.Length > 0 && sourceRowsByKey.TryGetValue(fields[0], out var source)
                ? SoftLineBreakJoiner.Apply(source, fields)
                : fields);
    }
}
