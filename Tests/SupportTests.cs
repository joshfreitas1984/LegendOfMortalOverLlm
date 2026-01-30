using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Xunit.Sdk;
using System.Security.Cryptography;
using Translate.Support;
using Translate.Utility;
using SweetPotato;

namespace Translate.Tests;

public class SupportTests
{
    const string workingDirectory = "../../../../Files";

    [Fact]
    public async Task GetNames()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        
        var names = new List<string>();

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
            {
                //CombatCharacter
                //CharacterTitle
                if ((line.Raw.StartsWith("Character/") || line.Raw.StartsWith("CombatCharacter/")) && line.Splits.Any())                   
                    if (!names.Contains(line.Splits[0].Text))
                        names.Add(line.Splits[0].Text);
            }

            await Task.CompletedTask;
        });

        Console.WriteLine($"Found {names.Count} names");

        var client = new HttpClient();
        var glossary = new List<string>();
        int batchSize = 10;

        for (int i = 0; i < names.Count; i += batchSize)
        {
            var batch = names.Skip(i).Take(batchSize).ToList();
            var tasks = batch.Select(async (name) =>
            {
                var trans = await TranslationService.TranslateInputAsync(client, config, name, 
                    GameTextFiles.TextFilesToSplit[0],
                    "Please avoid using words like 'the' and translate the following names of NPC: ");

                trans = LineValidation.CleanupLineBeforeSaving(trans, name, 
                    GameTextFiles.TextFilesToSplit[0], new StringTokenReplacer());
                lock (glossary)
                {
                    glossary.Add($"- raw: {name}");
                    glossary.Add($"  result: {trans}");
                    glossary.Add($"  badtrans: true");
                }
                Console.WriteLine($"Processed {name} - {trans}");
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/ExportGlossary.yaml", glossary);
    }

    [Fact]
    public async Task GetStats()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var items = new List<string>();

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
            {
          
                if (line.Raw.Contains("Stat") && line.Splits.Any())
                    if (!items.Contains(line.Splits[0].Text))
                        items.Add(line.Splits[0].Text);
            }

            await Task.CompletedTask;
        });

        Console.WriteLine($"Found {items.Count} stats");

        var client = new HttpClient();
        var glossary = new List<string>();
        int batchSize = 10;

        for (int i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            var tasks = batch.Select(async (name) =>
            {
                var trans = await TranslationService.TranslateInputAsync(client, config, name,
                    GameTextFiles.TextFilesToSplit[0],
                    "Please avoid using words like 'the' and translate the following game stats and statuses: ");

                trans = LineValidation.CleanupLineBeforeSaving(trans, name,
                    GameTextFiles.TextFilesToSplit[0], new StringTokenReplacer());
                lock (glossary)
                {
                    glossary.Add($"- raw: {name}");
                    glossary.Add($"  result: {trans}");
                    glossary.Add($"  badtrans: true");
                }
                Console.WriteLine($"Processed {name} - {trans}");
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/ExportStats.yaml", glossary);
    }

    [Fact]
    public async Task GetBook()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var items = new List<string>();

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
            {

                if ((line.Raw.StartsWith("Book/Title") || line.Raw.StartsWith("Book/Name")) && line.Splits.Any())
                    if (!items.Contains(line.Splits[0].Text))
                        items.Add(line.Splits[0].Text);
            }

            await Task.CompletedTask;
        });

        Console.WriteLine($"Found {items.Count} stats");

        var client = new HttpClient();
        var glossary = new List<string>();
        int batchSize = 10;

        for (int i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            var tasks = batch.Select(async (name) =>
            {
                var trans = await TranslationService.TranslateInputAsync(client, config, name,
                    GameTextFiles.TextFilesToSplit[0],
                    "Please avoid using words like 'the' and translate the following name of martial arts manuals: ");

                trans = LineValidation.CleanupLineBeforeSaving(trans, name,
                    GameTextFiles.TextFilesToSplit[0], new StringTokenReplacer());
                lock (glossary)
                {
                    glossary.Add($"- raw: {name}");
                    glossary.Add($"  result: {trans}");
                    glossary.Add($"  badtrans: true");
                }
                Console.WriteLine($"Processed {name} - {trans}");
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/ExportBooks.yaml", glossary);
    }

    [Fact]
    public async Task GetCombatSkill()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var items = new List<string>();

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
            {

                if ((line.Raw.StartsWith("CombatSkill/Name")) && line.Splits.Any())
                    if (!items.Contains(line.Splits[0].Text))
                        items.Add(line.Splits[0].Text);
            }

            await Task.CompletedTask;
        });

        Console.WriteLine($"Found {items.Count} stats");

        var client = new HttpClient();
        var glossary = new List<string>();
        int batchSize = 10;

        for (int i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            var tasks = batch.Select(async (name) =>
            {
                var trans = await TranslationService.TranslateInputAsync(client, config, name,
                    GameTextFiles.TextFilesToSplit[0],
                    "Please avoid using words like 'the' and translate the following name of martial arts skills: ");

                trans = LineValidation.CleanupLineBeforeSaving(trans, name,
                    GameTextFiles.TextFilesToSplit[0], new StringTokenReplacer());
                lock (glossary)
                {
                    glossary.Add($"- raw: {name}");
                    glossary.Add($"  result: {trans}");
                    glossary.Add($"  badtrans: true");
                }
                Console.WriteLine($"Processed {name} - {trans}");
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/ExportCombatSkills.yaml", glossary);
    }

    [Fact]
    public async Task GetPlayerTalent()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var items = new List<string>();

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
            {

                if ((line.Raw.StartsWith("PlayerTalent")) && line.Splits.Any())
                    if (!items.Contains(line.Splits[0].Text))
                        items.Add(line.Splits[0].Text);
            }

            await Task.CompletedTask;
        });

        Console.WriteLine($"Found {items.Count} stats");

        var client = new HttpClient();
        var glossary = new List<string>();
        int batchSize = 10;

        for (int i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            var tasks = batch.Select(async (name) =>
            {
                var trans = await TranslationService.TranslateInputAsync(client, config, name,
                    GameTextFiles.TextFilesToSplit[0],
                    "Please avoid using words like 'the' and translate the following name of player talents: ");

                trans = LineValidation.CleanupLineBeforeSaving(trans, name,
                    GameTextFiles.TextFilesToSplit[0], new StringTokenReplacer());
                lock (glossary)
                {
                    glossary.Add($"- raw: \"{name}\"");
                    glossary.Add($"  result: \"{trans}\"");
                    glossary.Add($"  badtrans: true");
                }
                Console.WriteLine($"Processed {name} - {trans}");
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/ExportPlayerTalent.yaml", glossary);
    }

    [Fact]
    public async Task GetUpgrade()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var items = new List<string>();

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
            {

                if ((line.Raw.StartsWith("Upgrade")) && line.Splits.Any())
                    if (!items.Contains(line.Splits[0].Text))
                        items.Add(line.Splits[0].Text);
            }

            await Task.CompletedTask;
        });

        Console.WriteLine($"Found {items.Count} stats");

        var client = new HttpClient();
        var glossary = new List<string>();
        int batchSize = 10;

        for (int i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            var tasks = batch.Select(async (name) =>
            {
                var trans = await TranslationService.TranslateInputAsync(client, config, name,
                    GameTextFiles.TextFilesToSplit[0],
                    "Please avoid using words like 'the' and translate the following name of upgrades: ");

                trans = LineValidation.CleanupLineBeforeSaving(trans, name,
                    GameTextFiles.TextFilesToSplit[0], new StringTokenReplacer());
                lock (glossary)
                {
                    glossary.Add($"- raw: \"{name}\"");
                    glossary.Add($"  result: \"{trans}\"");
                    glossary.Add($"  badtrans: true");
                }
                Console.WriteLine($"Processed {name} - {trans}");
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/ExportUpgrades.yaml", glossary);
    }

    [Fact]
    public async Task GetSystem()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        var items = new List<string>();

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
            {

                if ((line.Raw.StartsWith("System")) && line.Splits.Any())
                    if (!items.Contains(line.Splits[0].Text))
                        items.Add(line.Splits[0].Text);
            }

            await Task.CompletedTask;
        });

        Console.WriteLine($"Found {items.Count} stats");

        var client = new HttpClient();
        var glossary = new List<string>();
        int batchSize = 10;

        for (int i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            var tasks = batch.Select(async (name) =>
            {
                var trans = await TranslationService.TranslateInputAsync(client, config, name,
                    GameTextFiles.TextFilesToSplit[0],
                    "Please avoid using words like 'the' and translate the following name of menus: ");

                trans = LineValidation.CleanupLineBeforeSaving(trans, name,
                    GameTextFiles.TextFilesToSplit[0], new StringTokenReplacer());
                lock (glossary)
                {
                    glossary.Add($"- raw: \"{name}\"");
                    glossary.Add($"  result: \"{trans}\"");
                    glossary.Add($"  badtrans: true");
                }
                Console.WriteLine($"Processed {name} - {trans}");
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        File.WriteAllLines($"{workingDirectory}/TestResults/ExportSystem.yaml", glossary);
    }
}