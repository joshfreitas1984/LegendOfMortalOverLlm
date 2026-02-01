using System.Text;
using YamlDotNet.Serialization;

namespace Translate.Support;

public class GlossaryLine
{
    public string Raw { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;

    [YamlMember(Alias = "allowalt")]
    public List<string> AllowedAlternatives { get; set; } = [];
    public string Transliteration { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;

    [YamlMember(Alias = "misuse")]
    public bool CheckForMisusedTranslation { get; set; } = false;
    [YamlMember(Alias = "badtrans")]
    public bool CheckForBadTranslation { get; set; } = true;

    [YamlMember(Alias = "only")]
    public List<string> OnlyOutputFiles { get; set; } = [];

    [YamlMember(Alias = "exclude")]
    public List<string> ExcludeOutputFiles { get; set; } = [];

    public GlossaryLine()
    {
    }

    public GlossaryLine(string raw, string result)
    {
        Raw = raw;
        Result = result;
    }

    public static string AppendPromptsFor(string raw, List<GlossaryLine> glossaryLines, string outputFile)
    {
        var prompt = new StringBuilder();

        foreach (var line in glossaryLines)
        {
            //Exclusions and Targetted Glossary
            if (line.OnlyOutputFiles.Count > 0 && !line.OnlyOutputFiles.Contains(outputFile))
                continue;
            else if (line.ExcludeOutputFiles.Count > 0 && line.ExcludeOutputFiles.Contains(outputFile))
                continue;

            if (raw.Contains(line.Raw))
            {
                prompt.AppendLine(ToPromptString(line.Raw, line.Result));

                if (line.AllowedAlternatives != null)
                    foreach (var alternative in line.AllowedAlternatives)
                        prompt.AppendLine(ToPromptString(line.Raw, alternative));
            }
        }

        if (prompt.Length > 0)
            return prompt.ToString();
        else
            return string.Empty;
    }

    public static string ToPromptString(string raw, string translated)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"- raw: \"{raw}\"");
        prompt.AppendLine($"  result: \"{translated}\"");
        return prompt.ToString();
    }
}
