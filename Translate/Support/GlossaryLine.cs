using System.Text;
using YamlDotNet.Serialization;

namespace Translate.Support;

public class GlossaryLine
{
    public string Raw { get; set; } = string.Empty;
    public string RawSimplified { get; set; } = string.Empty;
    public string RawTraditional { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;


    [YamlMember(Alias = "allowalt")]
    public List<string> AllowedAlternatives { get; set; } = [];
    public string Direct { get; set; } = string.Empty;
    public string Literal { get; set; } = string.Empty;
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

        prompt.AppendLine("```");

        foreach (var line in glossaryLines)
        {
            //Exclusions and Targetted Glossary
            if (line.OnlyOutputFiles.Count > 0 && !line.OnlyOutputFiles.Contains(outputFile))
                continue;
            else if (line.ExcludeOutputFiles.Count > 0 && line.ExcludeOutputFiles.Contains(outputFile))
                continue;

            if (raw.Contains(line.Raw))
                prompt.AppendLine(ToPromptString(line.Raw, line.Result, line.AllowedAlternatives));
            else if (line.RawSimplified != string.Empty && raw.Contains(line.RawSimplified))
                prompt.AppendLine(ToPromptString(line.RawSimplified, line.Result, line.AllowedAlternatives));
            else if (line.RawTraditional != string.Empty && raw.Contains(line.RawTraditional))
                prompt.AppendLine(ToPromptString(line.RawTraditional, line.Result, line.AllowedAlternatives));
        }

        prompt.AppendLine("```");

        if (prompt.Length > 0)
            return prompt.ToString();
        else
            return string.Empty;
    }

    public static string ToPromptString(string raw, string translated, List<string>? alternatives)
    {
        var prompt = new StringBuilder();
        //prompt.AppendLine($"- raw: \"{raw}\"");
        //prompt.AppendLine($"  result: \"{translated}\"");

        //if (alternatives != null)
        //{ 
        //    foreach (var alternative in alternatives)
        //    {
        //        prompt.AppendLine($"- raw: \"{raw}\"");
        //        prompt.AppendLine($"  result: \"{alternative}\"");
        //    }
        //}

        //prompt.AppendLine($"- \"{raw}\": \"{translated}\"");

        prompt.AppendLine($"- raw: \"{raw}\"");
        //prompt.AppendLine($"  result: \"{translated}\"");
        prompt.AppendLine($"  result:");
        prompt.AppendLine($"    - \"{translated}\"");

        //if (alternatives != null)
        //    prompt.AppendLine($"  alternatives: \"{translated}\"");

        foreach (var alternative in alternatives ?? [])
            //prompt.AppendLine($"    - \"{alternative}\"");
            prompt.AppendLine($"    - \"{alternative}\"");

        return prompt.ToString();
    }
}
