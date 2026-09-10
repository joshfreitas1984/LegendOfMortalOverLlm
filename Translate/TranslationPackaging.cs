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
/// </summary>
public static class TranslationPackaging
{
    /// <summary>
    /// Reimplements the OLD Translate/FileOutputHandling.cs::PackageFinalTranslationAsync's
    /// LegendInfo-specific paragraph-newline handling as a rowPostProcess callback, scoped only to
    /// "LegendInfo.csv" (see CsvGameDataWorkflow.PackageAsync's rowPostProcess parameter).
    ///
    /// What the old code did (operating on the whole rebuilt CSV line's string, not per-column):
    ///   - "\\\\n\\\\n" (a doubled escaped-newline pair) -> real "\n\n" (a genuine paragraph break)
    ///   - "\\\\n" (single escaped-newline) -> " " (collapsed to a space)
    ///   - "\\n" (single escaped-newline, one backslash) -> " " (collapsed to a space)
    ///   - if the row's text field started with a literal space right after its opening quote
    ///     (i.e. the line contained ",\" "), that leading space was replaced with a blank line
    ///     (",\"\n\n") - restoring a paragraph break that had been flattened to a single space
    ///     during input handling.
    /// These only ever fired for LegendInfo rows in practice: the OLD input-handling step
    /// (FileInputHandling.cs) deliberately preserved literal "\n" sequences only for lines whose
    /// key started with "LegendInfo" (every other category had "\n" replaced with a space at dump
    /// time), so no other file's data could ever contain the "\\n"/"\\\\n" patterns being matched
    /// here.
    ///
    /// The new pipeline doesn't have an equivalent input-handling step that preserves raw "\n"
    /// escapes in the first place (CompoundFieldSplitter/CsvGameDataWorkflow don't special-case any
    /// one file), so whether LegendInfo.csv's real dumped text still contains these escape
    /// sequences at all is UNVERIFIED - NEEDS HUMAN REVIEW once the real game dump is available.
    /// This reimplementation operates on the reconstructed text column (assumed index 1, matching
    /// the old dump's 2-column "key,text" shape) rather than the old whole-line string, and treats
    /// "value starts with a single leading space" as the equivalent of the old ",\" " check.
    /// </summary>
    private static string[] CleanupLegendInfoParagraphBreaks(string[] splits)
    {
        if (splits.Length < 2)
            return splits;

        var text = splits[1]
            .Replace("\\\\n\\\\n", "\n\n")
            .Replace("\\\\n", " ")
            .Replace("\\n", " ");

        if (text.StartsWith(' '))
            text = "\n\n" + text[1..];

        splits[1] = text;
        return splits;
    }

    public static async Task PackageFinalTranslationAsync(string workingDirectory, TextFileToSplit[] textFiles)
    {
        string outputPath = $"{workingDirectory}/Mod";

        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true);

        Directory.CreateDirectory(outputPath);

        var passedCount = 0;
        var failedCount = 0;

        foreach (var textFile in textFiles.Where(t => t.TextFileType == TextFileType.RawCsv))
        {
            var (passed, failed) = await CsvGameDataWorkflow.PackageAsync(
                workingDirectory,
                textFile,
                rowPostProcess: textFile.Path == "LegendInfo.csv" ? CleanupLegendInfoParagraphBreaks : null);

            passedCount += passed;
            failedCount += failed;
        }

        Console.WriteLine($"Passed: {passedCount}");
        Console.WriteLine($"Failed: {failedCount}");
    }
}
