using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Translate.Utility;

namespace Translate.Tests;
public class PromptTuningTests
{
    const string workingDirectory = "../../../../Files";

    public class TranslatedRaw(string raw)
    {
        public string Raw { get; set; } = raw;
        public ValidationResult ValidationResult { get; set; } = new ValidationResult();
    }

    public static TextFileToSplit DefaultTestTextFile() => new TextFileToSplit()
    {
        Path = "",
    };

    [Fact(DisplayName = "1. Test Current Prompts")]
    public async Task TestPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        config.SkipLineValidation = true;
        config.RetryCount = 1;

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        config.RetryCount = 1;
        var batchSize = config.BatchSize ?? 50;

        var testLines = new List<TranslatedRaw> {
            new("而且我已经不再迷惘，已经在你的帮助下……更加坚定了。"),         
        };

        var cache = new Dictionary<string, string>();
        await TranslationService.FillTranslationCacheAsync(workingDirectory, 10, cache, config);       

        var results = new List<string>();
        var totalLines = testLines.Count;
        var stopWatch = Stopwatch.StartNew();

        for (int i = 0; i < totalLines; i += batchSize)
        {
            stopWatch.Restart();

            int batchRange = Math.Min(batchSize, totalLines - i);

            // Use a slice of the list directly
            var batch = testLines.GetRange(i, batchRange);

            int recordsProcessed = 0;

            // Process the batch in parallel
            await Task.WhenAll(batch.Select(async line =>
            {
                line.ValidationResult = await TranslationService.TranslateSplitAsync(config, line.Raw, client, DefaultTestTextFile());
                recordsProcessed++;
            }));

            var elapsed = stopWatch.ElapsedMilliseconds;
            var speed = recordsProcessed == 0 ? 0 : elapsed / recordsProcessed;
            Console.WriteLine($"Line: {i + batchRange} of {totalLines} ({elapsed} ms ~ {speed}/line)");
        }

        foreach (var line in testLines)
            results.Add($"From: {line.Raw}\nTo: {line.ValidationResult.Result}\nValid={line.ValidationResult.Valid}\n{line.ValidationResult.CorrectionPrompt}\n");

        File.WriteAllLines($"{workingDirectory}/TestResults/0.PromptTest.txt", results);
    }


    [Fact(DisplayName = "2. Optimise Provided Prompt")]
    public async Task OptimiseProvidedPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);

        // Prime the Request

        var basePrompt = config.Prompts["0PromptToOptimise"];
        var optimisePrompt = config.Prompts["0OptimisePrompt"];

        List<object> messages =
            [
                LlmHelpers.GenerateSystemPrompt(optimisePrompt),
                LlmHelpers.GenerateUserPrompt(basePrompt.ToString())
            ];

        // Generate based on what would have been created
        var result = await TranslationService.TranslateMessagesAsync(client, config, messages);

        File.WriteAllText($"{workingDirectory}/TestResults/1.MinimisePrompt.txt", result);
    }

    [Fact]
    public async Task NamePromptTest()
    {
        var textFile = DefaultTestTextFile();

        textFile.EnableBasePrompts = false;
        textFile.EnableGlossary = false;
        textFile.AdditionalPromptName = "FileRandomNamePrompt";

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "白亦";
        var result = await TranslationService.TranslateSplitAsync(config, input, client, textFile);

        File.WriteAllText($"{workingDirectory}/TestResults/2.NamePromptTest.txt", result.Result);
    }

    [Theory]
    [InlineData(1, "完成奇遇任务《轻功高手》")]
    [InlineData(2, "雄霸武林")]
    [InlineData(3, "德高望重重，才广武林称。兼备风云志，胸怀揽星辰。")]
    [InlineData(4, "于狂刀门贡献堂主处累积购买二十次")]
    [InlineData(5, "路过城西村时，遇到沈大娘正在收拾沈蛋，似乎是因为他将表姐家的衣服撕毁之事，沈蛋为了避免挨打，将竹条扔到了风车上面。")]
    [InlineData(6, "实力")]
    public async Task ExplainGlossaryPrompt(int index, string input)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);
        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain in a <think> why the glossary was or was not used. " +
            "How do I update the system prompt to make sure it uses the glossary in this case.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainGlossaryPrompt{index}.txt", result.Result);
    }

    [Fact]
    public async Task ExplainPrompt2()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "豆花嫂希望你能为她丈夫带来虎鞭，至于用途应该不难猜？";
        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasoning in a <think> tag at the end of the response. Also explain why the ? was removed. Also explain how to adjust the system prompt to correct it to make sure the '?' was not removed and context is retained.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainPrompt2.txt", result.Result);
    }

    [Fact]
    public async Task ExplainPrompt3()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "若果有此意，叫八戒伐几棵树来，沙僧寻些草来，我做木匠，就在这里搭个窝铺，你与她圆房成事，我们大家散了，却不是件事业？何必又跋涉，取什经去！";
        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasoning in a <think> tag at the end of the response. Also explain why the ! was removed. Also explain how to adjust the system prompt to correct it to make sure the '!' was not removed and context is retained. Show an example prompt.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainPrompt3.txt", result.Result);
    }

    [Fact]
    public async Task ExplainAlternativesPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);
        //var input = "好嘞，客官您慢走！";
        //var input = "完成菩提";
        //var input = "人阶";
        var input = "实力";
        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasoning in a <think> tag at the end of the response. " +
            "Explain if/why you provided an alternative." +           
            "Show where to update my current system prompt to stop the alternative and just give me one answer.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainAltPrompt.txt", result.Result);
    }

    [Fact]
    public async Task ExplainExplanationPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        var config = Configuration.GetConfiguration(workingDirectory);
        //var input = "好嘞，客官您慢走！";
        var input = "幽影-剑意纵横";

        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasoning in a <think> tag at the end of the response. " +
            "Explain if/why you provided an explanation." +
            "Also explain how to adjust the system prompt to correct it to make sure you do not provide this explanation." +
            "Show an example prompt.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainExplanationPrompt.txt", result.Result);
    }

    [Fact]
    public async Task ExplainNotEnglishPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "通关后天赋值";

        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasinging in a <explain> tag, why is the result is not in english." +
            "Show in a <prompt> tag, An updated system prompt that would have translated this to english.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainNotEnglishPrompt.txt", result.Result);
    }

    [Fact]
    public async Task ExplainRemovedCharsPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        var config = Configuration.GetConfiguration(workingDirectory);
        config.SkipLineValidation = true;
        config.RetryCount = 1;

        var input = "而且我已经不再迷惘，已经在你的帮助下……更加坚定了。";

        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasoning in a <explain> tag, why '...' characters were removed." +
            "Show in a <prompt> tag, the full updated current system prompt that would have not removed the '...' characters. " +
            "Highlight system prompt changes in a <changes> tag.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainRemovedCharsPrompt.txt", result.Result);
    }

    [Fact]
    public async Task ExplainMissingMarkupPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "操作 地面上双击并长按<w >施放 ";

        var prompts = TranslationService.GenerateBaseMessages(config, input, DefaultTestTextFile());

        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasinging in a <explain> tag, why the <w  > tag is missing." +
            "Show in a <prompt> tag, An updated system prompt that would have translated this to english.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainMissingMarkupPrompt.txt", result.Result);
    }

    [Fact]
    public async Task ExplainColorPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "在淮陵游玩之际，<color=&&00ff00ff>遇到一位自称烈火刀阎巧的侠客正在挑战淮陵豪侠</color>，我观其似乎武艺高强。";

        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),            
            "Explain your reasinging in a <explain> tag, why is there no <color> tag in the final result." +
            "Show in a <prompt> tag, An updated system prompt to ensure the <color> tag is included in the final result.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainColorPrompt.txt", result.Result);
    }

    [Fact]
    public async Task OptimiseCorrectTagPrompt()
    {
        var textFile = DefaultTestTextFile();

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);

        // Prime the Request
        var raw = "<color=#FF0000>炼狱</color>";
        var origResult = "Hellforge";
        var origValidationResult = LineValidation.CheckTransalationSuccessful(config, raw, origResult, textFile);
        List<object> messages = TranslationService.GenerateBaseMessages(config, raw, textFile);

        // Tweak Correction Prompt here
        var correctionPrompt = LineValidation.CalulateCorrectionPrompt(config, origValidationResult, raw, origResult);
        //var correctionPrompt = "Try again. The markup rules were not followed.";

        // Add what the correction prompt would have been
        TranslationService.AddCorrectionMessages(messages, origResult, correctionPrompt);

        var result = await TranslationService.TranslateMessagesAsync(client, config, messages);

        // Calculate output of test
        var validationResult = LineValidation.CheckTransalationSuccessful(config, raw, result, textFile);
        var lines = $"Valid:{validationResult.Valid}\nRaw:{raw}\nResult:{result}";
        File.WriteAllText($"{workingDirectory}/TestResults/OptimiseCorrectTag.txt", lines);
    }   
}
