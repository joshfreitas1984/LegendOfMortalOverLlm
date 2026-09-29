using FanslationStudio.LlmKit.Utility;
using SharedAssembly.TextResizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tests;

public class TextResizerTests
{
    const string workingDirectory = "../../../../Files";

    // The splitting itself lives in LlmKit (EditorFileSplitter) so every game packages the editor folders the same way.
    // Mortal has no layouts or sprites yet; the splitter ignores missing folders, so these are ready for when it does.
    [Fact]
    public static void MoveResizersIntoPathBasedFiles() => EditorFileSplitter.SplitResizers(workingDirectory);

    [Fact]
    public static void MoveSpritesIntoPathBasedFiles() => EditorFileSplitter.SplitSprites(workingDirectory);

    [Fact]
    public static void MoveLayoutsIntoPathBasedFiles() => EditorFileSplitter.SplitLayouts(workingDirectory);

    [Fact] // Can only be run when VS is running in admin
    public void CreateSymlinkToResizer()
    {
        var inputFolder = $"{workingDirectory}/Resizers";
        inputFolder = Path.GetFullPath(inputFolder);
        var outputFolder = @"C:\Program Files (x86)\Steam\steamapps\common\LegendOfMortal\BepInEx\resizers";

        if (Directory.Exists(outputFolder))
        {
            Console.WriteLine("Output folder already exists. Deleting it...");
            Directory.Delete(outputFolder, true);
        }

        // Run mklink command to create a symbolic link
        string command = $"/C mklink /D \"{outputFolder}\" \"{inputFolder}\"";
        ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", command)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas" // Run as administrator
        };

        Process process = new Process { StartInfo = psi };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Display output or error
        if (!string.IsNullOrEmpty(output))
            Console.WriteLine("Success: " + output);
        if (!string.IsNullOrEmpty(error))
            throw new Exception("Error: " + error);
    }

    [Fact] // Can only be run when VS is running in admin
    public void CreateSymlinkToAutoTrans()
    {
        var inputFolder = $"{workingDirectory}/AutoTranslator";
        inputFolder = Path.GetFullPath(inputFolder);
        var outputFolder = @"C:\Program Files (x86)\Steam\steamapps\common\LegendOfMortal\BepInEx\Translation\en\Text";

        if (Directory.Exists(outputFolder))
        {
            Console.WriteLine("Output folder already exists. Deleting it...");
            Directory.Delete(outputFolder, true);
        }

        // Run mklink command to create a symbolic link
        string command = $"/C mklink /D \"{outputFolder}\" \"{inputFolder}\"";
        ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", command)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas" // Run as administrator
        };

        Process process = new Process { StartInfo = psi };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Display output or error
        if (!string.IsNullOrEmpty(output))
            Console.WriteLine("Success: " + output);
        if (!string.IsNullOrEmpty(error))
            throw new Exception("Error: " + error);
    }

    [Fact]
    public void ReserializeResizerTest()
    {
        var serializer = YamlHelper.CreateSerializer();
        var deserializer = YamlHelper.CreateDeserializer();
        var folder = $"{workingDirectory}/Resizers";

        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var newResizers = deserializer.Deserialize<List<TextResizerContract>>(File.ReadAllText(file));
            var content = serializer.Serialize(newResizers);
            File.WriteAllText(file, content);
        }
    }
}
