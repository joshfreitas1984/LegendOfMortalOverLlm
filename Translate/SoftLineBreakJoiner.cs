using System.Text.RegularExpressions;

namespace Translate;

/// <summary>
/// Packaging-time fixup that removes the Chinese source's manual mid-sentence line wraps from the
/// English output. The game's descriptions (equipment, library, biographies, item/talent text, some
/// combat taunts and LegendInfo narration) break lines by hand to fit a fixed number of CJK
/// characters per line, e.g. "唐门弟子人手一柄的标准配备。\n外型看似与脱手标相去不远，\n实则是柄机关剑".
/// Kept verbatim, English gets ragged, unnatural breaks after every clause. A break is treated as a
/// soft wrap - and replaced with a space - only when the Chinese segment before it ends in '，',
/// '、' or '；'. Every other break (after a full stop/question/exclamation mark, a closing quote or
/// bracket, a "header：" line, a "[tag]", a list item, or a leading break) is left untouched.
///
/// Matching is done per column against the row's original Chinese cell (looked up by key), not the
/// translation: the break count of the source and the translation must agree, otherwise the row is
/// left as-is rather than guessing which English break maps to which Chinese one.
///
/// See docs/features/translation-pipeline/soft-line-break-joining.md for the affected files and
/// how to turn this off (<see cref="Enabled"/>).
/// </summary>
public static class SoftLineBreakJoiner
{
    /// <summary>Set to false to package every source line break verbatim again.</summary>
    public static bool Enabled { get; set; } = true;

    private const string SoftBreakEndings = "，、；";

    // The source always uses a literal two-character "\n"; the packaged translation can hold
    // either that or a real newline (PackagingTextFixups.FixLiteralNewline converts some).
    private static readonly Regex SourceBreak = new(@"\\n", RegexOptions.Compiled);
    private static readonly Regex TranslatedBreak = new(@"(\\n|\r?\n)", RegexOptions.Compiled);

    /// <summary>
    /// Returns <paramref name="translatedFields"/> with soft breaks joined, column by column, using
    /// <paramref name="sourceFields"/> (the same row's original Chinese fields) to decide which
    /// breaks are soft.
    /// </summary>
    public static string[] Apply(string[] sourceFields, string[] translatedFields)
    {
        var result = (string[])translatedFields.Clone();
        for (var i = 0; i < result.Length && i < sourceFields.Length; i++)
            result[i] = JoinField(sourceFields[i], result[i]);

        return result;
    }

    public static string JoinField(string source, string translated)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(translated))
            return translated;

        var sourceSegments = SourceBreak.Split(source);
        if (sourceSegments.Length < 2)
            return translated;

        // Split with a capture group keeps the separators at the odd indexes.
        var parts = TranslatedBreak.Split(translated);
        var translatedSegmentCount = (parts.Length + 1) / 2;
        if (translatedSegmentCount != sourceSegments.Length)
            return translated;

        var changed = false;
        for (var b = 0; b < sourceSegments.Length - 1; b++)
        {
            var before = sourceSegments[b].TrimEnd();
            if (before.Length == 0 || SoftBreakEndings.IndexOf(before[^1]) < 0)
                continue;

            var segmentIndex = b * 2;
            parts[segmentIndex] = parts[segmentIndex].TrimEnd();
            parts[segmentIndex + 1] = " ";
            parts[segmentIndex + 2] = parts[segmentIndex + 2].TrimStart();
            changed = true;
        }

        return changed ? string.Concat(parts) : translated;
    }
}
