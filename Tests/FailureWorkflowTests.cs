using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Translate.Support;
using Translate.Utility;

namespace Translate.Tests
{
    public class FailureWorkflowTests
    {
        const string workingDirectory = "../../../../Files";
        private const string FailingTransactionsPath = $"{workingDirectory}/TestResults/Failed/FailingTranslations.yaml";
        
        public static TextFileToSplit DefaultTestTextFile() => new TextFileToSplit()
        {
            Path = "",            
        };

        public class FailedTranslation
        {
            public string Text { get; set; }
            public string Translated { get; set; }
            public string Reason { get; set; }
        }

        [Fact]
        public async Task FindAllFailingTranslations()
        {

            var failures = new List<FailedTranslation>();
            var pattern = LineValidation.ChineseCharPattern;

            var forTheGlossary = new List<string>();

            await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
            {
                foreach (var line in fileLines)
                {
                    foreach (var split in line.Splits)
                    {
                        if (string.IsNullOrEmpty(split.Text))
                            continue;

                        // If it is already translated or just special characters return it
                        if (!Regex.IsMatch(split.Text, pattern))
                            continue;

                        if (!string.IsNullOrEmpty(split.Text) && (string.IsNullOrEmpty(split.Translated) || split.FlaggedForRetranslation))
                        {                            
                            failures.Add(new FailedTranslation
                            {
                                Text = split.Text,
                                Translated = split.Translated,
                                Reason = split.FlaggedMistranslation
                            });

                            if (split.Text.Length < 6)
                                if (!forTheGlossary.Contains(split.Text))
                                    forTheGlossary.Add(split.Text);
                        }
                    }
                }

                await Task.CompletedTask;
            });

            var serializer = Yaml.CreateSerializer();
            var yaml = serializer.Serialize(failures);
            File.WriteAllText(FailingTransactionsPath, yaml);

            File.WriteAllLines($"{workingDirectory}/TestResults/ForManualTrans.txt", forTheGlossary);

            //await TranslateFailedLinesForManualTranslation();
        }

        [Fact]
        public async Task TestExplainTagStripping()
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(300);
            var config = Configuration.GetConfiguration(workingDirectory);

            var serializer = Yaml.CreateDeserializer();
            var content = File.ReadAllText(FailingTransactionsPath);
            var failures = serializer.Deserialize<List<FailedTranslation>>(content);

            foreach (var failure in failures)
            {
                    var textFile = DefaultTestTextFile();
                    textFile.EnableBasePrompts = true;
                    textFile.EnableGlossary = true;

                    var messages = TranslationService.GenerateBaseMessages(config, failure.Text, textFile);
                    messages.Add(LlmHelpers.GenerateAssistantPrompt(failure.Translated));
                    messages.Add(LlmHelpers.GenerateUserPrompt(
                        @"You have removed a tag from translated text
Can you update the current system prompt and give me the full system prompt that would stop it from happening in future?"));

                    var result = await TranslationService.TranslateMessagesAsync(client, config, messages);

                    File.WriteAllText($"{workingDirectory}/TestResults/Failed/TestExplain.txt", result);

                    return;
            }
        }

        [Fact]
        public async Task RetestNewSystemPrompts()
        {
            bool isManual = false;

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(300);
            var config = Configuration.GetConfiguration(workingDirectory);

            //config.SkipLineValidation = true;

            var serializer = Yaml.CreateDeserializer();
            var content = File.ReadAllText(FailingTransactionsPath);
            var failures = serializer.Deserialize<List<FailedTranslation>>(content);

            foreach (var failure in failures)
            {
                //if (Regex.IsMatch(failure.Translated, LineValidation.ChineseCharPattern))
                {
                    var textFile = DefaultTestTextFile();
                    textFile.EnableGlossary = false;

                    if (isManual)
                    {
                        var messages = TranslationService.GenerateBaseMessages(config, failure.Text, textFile);

                        var result = await TranslationService.TranslateMessagesAsync(client, config, messages);
                        File.WriteAllText($"{workingDirectory}/TestResults/Failed/RetestNewSystemPrompts.txt", result);


                        if (Regex.IsMatch(result, LineValidation.ChineseCharPattern))
                            Assert.Fail("The new system prompt did not work, it is still adding Chinese characters");
                    }
                    else
                    {
                        var result = await TranslationService.TranslateSplitAsync(config, failure.Text, client, DefaultTestTextFile());
                        File.WriteAllText($"{workingDirectory}/TestResults/Failed/RetestNewSystemPrompts.txt", result.Result);
                    }

                    return;
                }
            }
        }
    }
}
