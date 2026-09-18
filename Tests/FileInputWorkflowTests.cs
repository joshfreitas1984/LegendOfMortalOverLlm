using FanslationStudio.LlmKit;
using Translate;

namespace Tests;

public class FileInputWorkflowTests
{
    [Fact(DisplayName = "1. ExportAssetsIntoTranslated")]
    public void ExportAssetsIntoTranslated()
    {
        TranslationExport.ExportGameSpecificTextAssetsToCustomFormat(GameFileHandling.WorkingDirectory);
    }

    [Fact(DisplayName = "1a. Migrate old StringTable translations into new per-file Converted")]
    public void MigrateOldTranslationsIntoNewFiles()
    {
        // Permanent/repeatable siphon of already-translated text from the OLD, pre-migration
        // Files/Converted/StringTable.csv.yaml donor into the new per-category Converted/*.yaml
        // files - see LegacyDataMigration's doc comment. Safe to re-run any time (only ever fills
        // currently-blank Translated values, never overwrites an existing one).
        LegacyDataMigration.MigrateOldTranslationsIntoNewFiles(GameFileHandling.WorkingDirectory);
    }

    [Fact(DisplayName = "99. MergeFilesIntoTranslated")]
    public async Task MergeFilesIntoTranslated()
    {
        await GameFileHandlingBase.MergeFilesIntoTranslatedAsync(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit);
    }

    [Fact(DisplayName = "999. Check File Lines Match")]
    public void CheckFileLinesMatch()
    {
        var badFiles = GameFileHandlingBase.CheckFileLinesMatch(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit);
        Assert.Empty(badFiles);
    }
}
