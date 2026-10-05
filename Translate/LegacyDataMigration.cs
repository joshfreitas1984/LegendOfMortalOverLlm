using FanslationStudio.LlmKit.Support;
using FanslationStudio.LlmKit.Utility;
using System.Text.RegularExpressions;
using ToolGood.Words;

namespace Translate;

/// <summary>
/// Permanent, repeatable/idempotent migration ("siphon") routine that copies already-translated
/// text out of the OLD, pre-migration, single merged Files/Raw/StringTable.csv /
/// Files/Converted/StringTable.csv.yaml donor pair into the NEW per-category
/// Files/Converted/{file}.csv.yaml files produced by <see cref="TranslationExport"/>, so the years
/// of existing translation work aren't lost just because the pipeline now splits the game's text
/// into many small per-category files instead of one merged file.
///
/// Files/Raw/StringTable.csv and Files/Converted/StringTable.csv.yaml are NEVER deleted or
/// modified by this routine (or anything else in this migration) - they stay exactly as they are,
/// permanently, purely as a translation-data donor for this routine to read from.
///
/// The OLD Converted/StringTable.csv.yaml uses the OLD, hand-rolled
/// (pre-FanslationStudio.LlmKit) TranslationLine/TranslationSplit shape from the deleted
/// Translate/Support/TranslationLine.cs and Translate/Support/TranslationSplit.cs (NOT
/// FanslationStudio.LlmKit.Support's types of the same name) - reproduced here as small private
/// deserialization-only shapes (<see cref="OldTranslationLine"/>/<see cref="OldTranslationSplit"/>)
/// since only Raw/Splits[].Text/Splits[].Translated are needed for this lookup. The OLD
/// StringTable.csv was a plain 2-column "key,\"text\"" CSV; the OLD FileInputHandling.cs decomposed
/// column 1 by naively splitting the whole (quote-stripped) line on ',' and treating every
/// resulting piece containing Chinese characters as its own TranslationSplit - it did NOT use
/// anything like the new CompoundFieldSplitter.Decompose's structured fragment/template model, so
/// an old line's Splits do not necessarily line up positionally with a new line's Splits (a new
/// per-column fragment index vs the old naive whole-line comma-split index). When a line matched
/// by key happens to have the same split COUNT on both sides, position is trusted directly (both
/// splitters walk the same original cell left-to-right, so an equal count means an equal shape) -
/// this is the common case and needs no text comparison at all. Only when the counts differ is a
/// split paired up by comparing its raw <see cref="TranslationSplit.Text"/> content against the
/// old line's splits, since then there's no other way to tell which fragment corresponds to which.
///
/// The old StringTable.csv donor was dumped from the game's ChineseTraditional source, while the
/// new per-category dumps are ChineseSimplified only (see StringTableDumpPatches.TargetLanguage),
/// so an old split's Text never equals its matching new split's Text verbatim - both sides are
/// normalized to Simplified (via ToolGood.Words' WordsHelper, see <see cref="NormalizeForComparison"/>)
/// before comparing. A handful of Taiwanese vocabulary variants (e.g. "妳" vs "你") survive that
/// script-level conversion untouched since they're distinct codepoints in both scripts rather than
/// simplified/traditional forms of each other - <see cref="VariantNormalizations"/> folds the known
/// ones on top.
/// </summary>
public static class LegacyDataMigration
{
    private class OldTranslationSplit
    {
        public int Split { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Translated { get; set; } = string.Empty;
    }

    private class OldTranslationLine
    {
        public string Raw { get; set; } = string.Empty;
        public List<OldTranslationSplit> Splits { get; set; } = [];
    }

    // Taiwanese/Traditional vocabulary variants that WordsHelper.ToSimplifiedChinese (a plain
    // character-level t2s mapping) doesn't fold - these are distinct codepoints in both scripts,
    // not simplified/traditional forms of each other, so the library correctly leaves them alone.
    // Applied on top of ToSimplifiedChinese when comparing old vs new split text so pairs like the
    // old donor's "妳" (female-specific "you") vs the new dump's "你" still match. Extend as more
    // of these turn up.
    private static readonly Dictionary<string, string> VariantNormalizations = new()
    {
        ["妳"] = "你",
        ["祢"] = "你",
    };

    private static string NormalizeForComparison(string text)
    {
        var normalized = WordsHelper.ToSimplifiedChinese(text);
        foreach (var (from, to) in VariantNormalizations)
            normalized = normalized.Replace(from, to);

        return normalized;
    }

    // The old donor translated the whole cell (e.g. "Late +1" for "下旬+1"), but the new splitter
    // moves the literal parts ("+1") into the line's template, which packaging re-applies around
    // the fragment's translation - so a donor value carrying them would ship doubled ("Late +1+1").
    // Strips the template's literal prefix/suffix (or its trailing/leading punctuation run, since the
    // donor often ASCII-fied it, e.g. "：？？？" -> ": ???") from a donor value copied into a templated
    // single-placeholder split.
    private static string StripTemplateLiterals(string translated, TranslationLine line, TranslationSplit split)
    {
        var template = line.Templates.FirstOrDefault(t => t.Split == split.Split)?.Template;
        if (template == null || template.Split("{0}") is not [var prefix, var suffix] || template.Contains("{1}"))
            return translated;

        prefix = prefix.Trim();
        suffix = suffix.Trim();

        if (prefix.Length > 0)
            translated = translated.StartsWith(prefix, StringComparison.Ordinal)
                ? translated[prefix.Length..]
                : Regex.Replace(translated, @"^[^\w\s]+\s*", string.Empty);

        if (suffix.Length > 0)
            translated = translated.EndsWith(suffix, StringComparison.Ordinal)
                ? translated[..^suffix.Length]
                : Regex.Replace(translated, @"\s*[^\w\s]+$", string.Empty);

        return translated.Trim();
    }

    /// <summary>
    /// For every configured, already-exported new per-file Converted/{file}.csv.yaml, fills any
    /// currently-blank <see cref="TranslationSplit.Translated"/> from the matching OLD donor line's
    /// split (matched by identical raw split <see cref="TranslationSplit.Text"/>), when a matching
    /// old line can be found by key. Only ever fills blanks - never overwrites an already-translated
    /// new-pipeline value, so it's always safe to re-run (e.g. after a re-export adds new rows)
    /// without clobbering later manual/QC corrections.
    /// </summary>
    public static void MigrateOldTranslationsIntoNewFiles(string workingDirectory)
    {
        var oldFile = $"{workingDirectory}/Converted/StringTable.csv.yaml";
        if (!File.Exists(oldFile))
        {
            Console.WriteLine("LegacyDataMigration: old Converted/StringTable.csv.yaml donor file not found - nothing to migrate.");
            return;
        }

        var deserializer = YamlHelper.CreateDeserializer();
        var serializer = YamlHelper.CreateSerializer();

        var oldLines = deserializer.Deserialize<List<OldTranslationLine>>(File.ReadAllText(oldFile)) ?? [];

        // Index old lines by their raw CSV key (column 0 - "Category/subkey", or a handful of
        // legacy keys with no "/" at all).
        var oldByKey = new Dictionary<string, OldTranslationLine>();
        foreach (var line in oldLines)
        {
            //if (line.Raw.StartsWith("Story/"))
            //    line.Raw = line.Raw.Replace("\n", "");

            var oldCols = CompoundFieldSplitter.ParseCsvRow(line.Raw);
            if (oldCols.Length == 0)
                continue;

            oldByKey.TryAdd(oldCols[0], line);
        }

        foreach (var textFile in TextFileConfiguration.TextFilesToSplit)
        {
            var newFile = $"{workingDirectory}/Converted/{textFile.Path}.yaml";
            if (!File.Exists(newFile))
                continue;

            var newLines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText(newFile)) ?? [];

            // "Story.csv" -> "Story" - the old donor's key prefix for this category, used as a
            // fallback if the new per-file dump's own key turns out to still carry that prefix
            // (unverified - see TextFileConfiguration.cs's PROVISIONAL note; this covers both
            // possible real-world key shapes rather than assuming one).
            var category = Path.GetFileNameWithoutExtension(textFile.Path);
            var filledCount = 0;

            foreach (var newLine in newLines)
            {
                var newCols = CompoundFieldSplitter.ParseCsvRow(newLine.Raw);
                if (newCols.Length == 0)
                    continue;

                var newKey = newCols[0];

                if (!oldByKey.TryGetValue(newKey, out var oldLine)
                    && !oldByKey.TryGetValue($"{category}/{newKey}", out oldLine))
                    continue;

                // Both the old (naive whole-line comma-split) and new (CompoundFieldSplitter,
                // template-aware) splitters walk the same original cell left-to-right, so when
                // they happen to produce the same number of fragments for a line, position alone
                // is a reliable pairing - trust the key match and skip text comparison entirely.
                // This is what lets a line survive even when its split text was never going to
                // compare equal (Traditional/Simplified script differences beyond what
                // NormalizeForComparison folds, punctuation/whitespace the two splitters handle
                // differently, etc.) - the key match already told us these are the same line.
                // Only fall back to matching by (normalized) text when the fragment counts differ,
                // since then there's no other way to tell which old fragment corresponds to which
                // new one.
                var samePositionCount = oldLine.Splits.Count == newLine.Splits.Count;

                for (var i = 0; i < newLine.Splits.Count; i++)
                {
                    var split = newLine.Splits[i];

                    // Only fill blanks - never overwrite an already-translated new-pipeline value.
                    if (!string.IsNullOrEmpty(split.Translated))
                        continue;

                    var oldSplit = samePositionCount
                        ? oldLine.Splits[i]
                        : oldLine.Splits.FirstOrDefault(s => NormalizeForComparison(s.Text) == NormalizeForComparison(split.Text));

                    if (oldSplit == null || string.IsNullOrEmpty(oldSplit.Translated))
                        continue;

                    split.Translated = StripTemplateLiterals(oldSplit.Translated, newLine, split);
                    filledCount++;
                }
            }

            if (filledCount > 0)
            {
                FileHelper.WriteAllTextWithRetry(newFile, serializer.Serialize(newLines));
                Console.WriteLine($"LegacyDataMigration: filled {filledCount} blank translation(s) into {textFile.Path} from the old StringTable.csv donor.");
            }
        }
    }
}
