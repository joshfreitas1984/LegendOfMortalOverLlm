using System.IO.Compression;
using Translate.Utility;
using static SweetPotato.FileView;

namespace Translate.Tests;

public class FileOutputWorkflowTests
{
    public const string WorkingDirectory = TranslationWorkflowTests.WorkingDirectory;
    public const string GameFolder = TranslationWorkflowTests.GameFolder;


    [Fact(DisplayName = "6. Package to Game Files")]
    public static async Task PackageFinalTranslation()
    {
        await FileOutputHandling.PackageFinalTranslationAsync(WorkingDirectory);

        var sourceDirectory = $"{WorkingDirectory}/Mod/";
        var modDirectory = $"{GameFolder}/Mods/";

        if (Directory.Exists(modDirectory))
            Directory.Delete(modDirectory, true);

        FileOutputHandling.CopyDirectory(sourceDirectory, modDirectory);
    }

    [Fact(DisplayName = "7. Zip Release")]
    public static async Task ZipRelease()
    {
        var version = ModHelper.CalculateVersionNumber();

        string releaseFolder = $"{GameFolder}/ReleaseFolder/Files";

        File.Copy($"{WorkingDirectory}/Mod/English/StringTable.csv", $"{releaseFolder}/Mods/English/StringTable.csv", true);

        ZipFile.CreateFromDirectory($"{releaseFolder}", $"{releaseFolder}/../EnglishPatch-{version}.zip");
        await Task.CompletedTask;
    }
}
