namespace Translate;

public class GameTextFiles
{
    // "。" doesnt work like u think it would   
    public static string[] SplitCharactersList = [
            "\\n",
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
            
            //"-", // This will split between other groups
            ":", // This will split between other groups
            //"|",
            //"。"
            //"<br>",
            //"-", 
        ];

    public static string[] SplitRegexPatterns = [
        //@"(.*?)",
        //@"（.*?）",
        //@"《.*?》",
        //@"\〈.*?\〉",
        //@"\「.*?\」",
        //@"\『.*?\』",
        //@"\【.*?\】",
        //@"\〖.*?\〗",
        //@"\“.*?”"
    ];
    public static string[] FilesNotHandled = [
    ];

    public static readonly TextFileToSplit[] TextFilesToSplit = [
        //Biggest one
        new() {Path = "StringTable.csv", PackageOutput = true, IgnoreHtmlTagsInText = true }
    ];
}
