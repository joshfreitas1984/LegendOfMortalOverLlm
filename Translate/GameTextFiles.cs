namespace Translate;

public class GameTextFiles
{
    // "。" doesnt work like u think it would   
    public static string[] SplitCharactersList = [
            // "。"
            // ":", "<br>", "\\n", "-", "|"
        ];

    public static string[] FilesNotHandled = [
    ];

    public static readonly TextFileToSplit[] TextFilesToSplit = [
        //Biggest one
        new() {Path = "StringTable.csv", PackageOutput = true, IgnoreHtmlTagsInText = true }
    ];
}
