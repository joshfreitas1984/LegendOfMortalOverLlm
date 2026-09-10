using FanslationStudio.LlmKit.Support;
using FanslationStudio.LlmKit.Utility;

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
/// per-column fragment index vs the old naive whole-line comma-split index). Matching is therefore
/// done by comparing each split's raw <see cref="TranslationSplit.Text"/> content within a
/// line matched by key, rather than assuming the two Splits lists share the same shape/order.
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

                foreach (var split in newLine.Splits)
                {
                    // Only fill blanks - never overwrite an already-translated new-pipeline value.
                    if (!string.IsNullOrEmpty(split.Translated))
                        continue;

                    var oldSplit = oldLine.Splits.FirstOrDefault(s => s.Text == split.Text);
                    if (oldSplit == null || string.IsNullOrEmpty(oldSplit.Translated))
                        continue;

                    split.Translated = oldSplit.Translated;
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
