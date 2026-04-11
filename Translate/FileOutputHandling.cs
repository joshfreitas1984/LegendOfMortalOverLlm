using SharedAssembly.DynamicStrings;
using System.Text.RegularExpressions;
using Translate;
using Translate.Utility;

public class FileOutputHandling
{
    public static async Task PackageFinalTranslationAsync(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Converted";
        string outputPath = $"{workingDirectory}/Mod";

        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true);

        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory($"{outputPath}/English");

        var finalDb = new List<string>();
        var passedCount = 0;
        var failedCount = 0;

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            var failedLines = new List<string>();
            var outputLines = new List<string>();

            foreach (var line in fileLines)
            {
                // Regular DB handling
                var splits = line.Raw.Split(',');
                var failed = false;

                foreach (var split in line.Splits)
                {
                    if (!textFileToTranslate.PackageOutput
                        || split.FlaggedForRetranslation
                        || !split.SafeToTranslate) //Count Failure
                    {
                        failed = true;
                        break;
                    }

                    //Check line to be extra safe
                    if (Regex.IsMatch(split.Translated, @"(?<!\\)\n"))
                        failed = true;
                    else if (!string.IsNullOrEmpty(split.Translated))
                        splits[split.Split] = $"\"{split.Translated}\"";
                    //If it was already blank its all good
                    else if (!string.IsNullOrEmpty(split.Text))
                        failed = true;
                }

                line.Translated = string.Join(',', splits);

                if (!failed)
                {
                    //LegendInfo/S0801_02_001,"\nYou visited Ye Yunzhao and always felt that he had a better chance of winning when going up against the Alliance Leader. However, hearing that he single‑handedly faced off against the Dual Sages of Diancang and narrowly avoided disaster made you realize how precarious the situation was. Fortunately, a skilled ally arrived to help, leaving the Diancang Dual Sages with no ground to stand on. Watching Yunzhang cry bitterly, you couldn't help but feel relieved.\nIf Ye Yunzhao unfortunately meets his end at the hands of others, I truly wonder how he would explain it to Yunshang."

                    // Handle cleaning LegendInfo
                    var cleanedLine = line.Translated
                        .Replace("\\\\n\\\\n", "\n\n")
                        .Replace("\\\\n", " ")
                        .Replace("\\n", " ");

                    var charIndex = cleanedLine.IndexOf(",\" ");
                    if (charIndex != -1)
                        cleanedLine = cleanedLine.Remove(charIndex, 3).Insert(charIndex, ",\"\n\n");

                    // cleanedLine = line.Translated;
                    outputLines.Add(cleanedLine);
                }
                else
                {
                    var rawSplits = line.Raw.Split(",");
                    //if (rawSplits.Length < 2)
                    //    outputLines.Add($"{rawSplits[0]},\"\"");
                    //else
                    outputLines.Add($"{rawSplits[0]},\"{rawSplits[1]}\"");

                    failedLines.Add(line.Raw);
                }
            }


            File.WriteAllLines($"{outputPath}/English/{textFileToTranslate.Path}", outputLines);

            passedCount += outputLines.Count;
            failedCount += failedLines.Count;

            await Task.CompletedTask;
        });


        Console.WriteLine($"Passed: {passedCount}");
        Console.WriteLine($"Failed: {failedCount}");
    }

    public static void CopyDirectory(string sourceDir, string destDir, bool overwrite = false)
    {
        // Get the subdirectories for the specified directory.
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");

        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var tempPath = Path.Combine(destDir, file.Name);
            file.CopyTo(tempPath, overwrite);
        }

        // Copy each subdirectory using recursion
        DirectoryInfo[] dirs = dir.GetDirectories();
        foreach (DirectoryInfo subdir in dirs)
        {
            if (subdir.Name == ".git" || subdir.Name == ".vs")
                continue;

            var tempPath = Path.Combine(destDir, subdir.Name);
            CopyDirectory(subdir.FullName, tempPath);
        }
    }
}