using System.Text;
using System.Text.RegularExpressions;
using Translate.Utility;

namespace Translate;

public class InputFileHandling
{
    public static void ExportTextAssetsToCustomFormat(string workingDirectory)
    {
        string outputPath = $"{workingDirectory}/Raw/Export";
        string convertedPath = $"{workingDirectory}/Converted";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        if (!Directory.Exists(convertedPath))
            Directory.CreateDirectory(convertedPath);

        var serializer = Yaml.CreateSerializer();
        var pattern = LineValidation.ChineseCharPattern;

        var dir = new DirectoryInfo($"{workingDirectory}/Raw");
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            //var lines = File.ReadAllLines(file.FullName);

            // Step 1: Read the entire file as a single string
            string fileContent = File.ReadAllText(file.FullName);

            // Step 2: Use a regex to find quoted fields and replace any \n or \r\n inside them with a space
            // This regex matches quoted fields, including those with escaped quotes ("")
            string patternQuotes = "\"((?:[^\"]|\"\")*)\"";
            string cleanedContent = Regex.Replace(fileContent, patternQuotes, match =>
            {
                // Replace newlines inside quoted fields with a space
                string quoted = match.Groups[1].Value.Replace("\r\n", " ").Replace("\n", " ");
                // Restore double quotes if present
                quoted = quoted.Replace("\"\"", "\"");
                return $"\"{quoted}\"";
            });

            // Step 3: Split the cleaned content into lines
            var lines = cleanedContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            File.WriteAllLines($"{outputPath}/{file.Name}.stripped.csv", lines);

            var foundLines = new List<TranslationLine>();
            var lineIncrement = 0;

            foreach (var line in lines)
            {
                lineIncrement++;

                // Clean quotes
                var newLine = line.Replace("\"", string.Empty);

                var splits = newLine.Split(",");
                var foundSplits = new List<TranslationSplit>();

                // Find Chinese
                for (int i = 0; i < splits.Length; i++)
                {
                  

                    if (Regex.IsMatch(splits[i], pattern))
                    {
                        foundSplits.Add(new TranslationSplit()
                        {
                            Split = i,
                            Text = splits[i],
                        });
                    }
                }

                //The translation line
                foundLines.Add(new TranslationLine()
                {
                    //LineNum = lineNum,
                    Raw = newLine,
                    Splits = foundSplits,
                });
            }

            // Write the found lines
            var yaml = serializer.Serialize(foundLines);
            File.WriteAllText($"{outputPath}/{file.Name}", yaml);

            // Add missing converted file if it doesnt exist yet
            if (!File.Exists($"{convertedPath}/{file.Name}"))
                File.Copy($"{outputPath}/{file.Name}", $"{convertedPath}/{file.Name}");
        }
    }
      
    public static async Task MergeFilesIntoTranslatedAsync(string workingDirectory)
    {
        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            var newCount = 0;

            ////Disable for now since they should be same
            //if (textFileToTranslate.TextFileType == TextFileType.RegularDb)
            //    return;

            var deserializer = Yaml.CreateDeserializer();
            var exportFile = outputFile.Replace("Converted", "Raw/Export");
            var exportLines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText(exportFile));

            foreach (var line in exportLines)
            {
                var found = fileLines.FirstOrDefault(x => x.Raw == line.Raw);
                if (found != null)
                {
                    foreach (var split in line.Splits)
                    {
                        var found2 = found.Splits.FirstOrDefault(x => x.Text == split.Text);
                        if (found2 != null)
                            split.Translated = found2.Translated;
                    }
                }
                else
                {
                    // Try matching on split instead of line incase they changed line format
                    foreach (var split in line.Splits)
                    {
                        var found2 = fileLines
                            .Select(x => x.Splits.FirstOrDefault(s => s.Text == split.Text))
                            .FirstOrDefault(s => s != null);

                        if (found2 != null)
                            split.Translated = found2.Translated;
                        else
                            newCount++;
                    }
                }
            }

            Console.WriteLine($"New Lines {textFileToTranslate.Path}: {newCount}");

            //if (newCount > 0 || exportLines.Count != fileLines.Count) //Always Write because they might have changed format
            {
                var serializer = Yaml.CreateSerializer();
                File.WriteAllText(outputFile, serializer.Serialize(exportLines));
            }

            await Task.CompletedTask;
        });
    }
}
