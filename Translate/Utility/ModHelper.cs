using System.Text.Json;

namespace Translate.Utility;

public class ModConfigFile
{
    public long PublishedFileId { get; set; } = 3432636639;
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = "English Patch";
    public string ContentFolder { get; set; } = ModHelper.ContentFolder;
    public string ChangeNote { get; set; } = string.Empty;
    public string Description { get; set; } = "English Translation using an LLM\n Resizer required to fit text. \nCome hang in our Discord: https://discord.gg/sqXd5ceBWT";
    public string PreviewUrl { get; set; } = "H:\\Xyzj2OverLlm\\Files\\Mod\\preview.png";
    public string MetaData { get; set; } = "";
    public int Visibility { get; set; } = 0;
    public long ModId { get; set; } = 1740293083;
    public string Tags { get; set; } = "[\"\\u5176\\u4ED6\"]";
}

public static class ModHelper
{
    public const string ContentFolder = "LashEnglishPatch";

    public static string CalculateVersionNumber() => DateTime.Now.ToString("yyyy.MM.dd.HH.mm");

    public static void GenerateModConfig(string workingDirectory)
    {
        var config = new ModConfigFile()
        {
            ChangeNote = $"Version: {CalculateVersionNumber()}",
        };

        var outputFile = $"{workingDirectory}/Mod/{ContentFolder}/workshop.json";

        var lines = JsonSerializer.Serialize(config,
            new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true // Optional: Makes JSON more readable
            });

        using (FileStream fileStream = new FileStream(outputFile, FileMode.Create))
        using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
            binaryWriter.Write(lines);

        //File.WriteAllBytes(outputFile, lines);
    }
}
