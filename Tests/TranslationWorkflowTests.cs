using FanslationStudio.LlmKit.Configuration;
using FanslationStudio.LlmKit.Utility;
using FanslationStudio.LlmKit.Workflow;
using Translate;
using System.Text.RegularExpressions;
using static FanslationStudio.LlmKit.GameFileHandlingBase;

namespace Tests;

public class TranslationWorkflowTests
{
    [Fact(DisplayName = "0. Reset All Flags")]
    public async Task ResetAllFlags()
    {
        await TranslationWorkflow.ResetAllFlags(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit);
    }

    [Fact(DisplayName = "1. TranslateLinesBruteForce")]
    public async Task TranslateLinesBruteForce()
    {
        await TranslationWorkflow.TranslateLinesBruteForce(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, GameFileHandling.Hooks);
        await FileOutputWorkflowTests.PackageFinalTranslation();
    }

    [Fact(DisplayName = "2. ApplyRulesToCurrentTranslation")]
    public async Task ApplyRulesToCurrentTranslation()
    {
        await TranslationWorkflow.ApplyAllRulesToCurrentTranslation(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, GameFileHandling.Hooks);
    }

    [Fact(DisplayName = "3. Translate Lines Only")]
    public async Task TranslateLines()
    {
        await TranslationWorkflow.TranslateLines(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, GameFileHandling.Hooks);
        await FileOutputWorkflowTests.PackageFinalTranslation();
    }

    // Finds translations with an invented gender (he/she where the source states none) or subject-less narration
    // written as "I". The dry run writes only TestResults/PronounRetranslation.yaml; run it first to read the count.
    // This game has no LineContextProvider, so speaker-gender checks are not available, and its prose setting
    // (lines whose translation names a character are skipped) comes from Config.yaml pronounCheck.
    [Fact(DisplayName = "5. Count lines needing pronoun retranslation (dry run)")]
    public async Task CountPronounRetranslation() =>
        await PronounDefectWorkflow.RunAsync(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit,
            flagForRetranslation: false, GameFileHandling.Hooks);

    // Also sets FlaggedForRetranslation on each hit. Flagged lines are not packaged until retranslated, so follow it
    // straight away with a translate-flagged run.
    [Fact(DisplayName = "5. Flag lines needing pronoun retranslation")]
    public async Task FlagPronounRetranslation() =>
        await PronounDefectWorkflow.RunAsync(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit,
            flagForRetranslation: true, GameFileHandling.Hooks);

    [Fact(DisplayName = "4. Find All Failing Translations")]
    public async Task FindAllFailingTranslations()
    {
        var workingDirectory = GameFileHandling.WorkingDirectory;
        (List<FailedTranslation> failures, List<string> forTheGlossary) =
            await GetFailedTranslations(workingDirectory, TextFileConfiguration.TextFilesToSplit);

        var serializer = YamlHelper.CreateSerializer();
        var yaml = serializer.Serialize(failures);
        FileHelper.WriteAllTextWithRetry($"{workingDirectory}/TestResults/FailedTranslations.yaml", yaml);
        FileHelper.WriteAllLinesWithRetry($"{workingDirectory}/TestResults/ForManualTrans.yaml", forTheGlossary);
    }

    [Fact(DisplayName = "5. Flag some regexes")]
    public async Task SetSplitAsInvalid()
    {
        var badStrings = new List<string>
        {
            "⑩",
            "⓪",
            "①",
            "②",
            "③",
            "④",
            "⑤",
            "⑥",
            "⑦",
            "⑧",
            "⑨",

            "《",
            "〈",
            "「",
            "『",
            "【",
            "〖",
            "“",
        };

        await TranslationWorkflow.SetSplitAsInvalid(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, badStrings);
    }

    [Fact(DisplayName = "6. Clean up some regexes")]
    public static async Task CleanUpSomeRegexes()
    {
        var regex = new List<(string pattern, string replacement)>
        {
            // Look for Number then "coin" or "wen" or "money" or "quan" or "liang", get the number portion
            (@"(\d+)(\s*)(coin|wen|money|quan|liang)", "$1 coin"),
        };

        await TranslationWorkflow.CleanUpSomeRegexes(GameFileHandling.WorkingDirectory, TextFileConfiguration.TextFilesToSplit, regex);
    }
}
