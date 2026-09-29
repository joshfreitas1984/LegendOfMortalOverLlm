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
