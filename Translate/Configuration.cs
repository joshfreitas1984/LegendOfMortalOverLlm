using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Translate.Support;
using Translate.Utility;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Translate;

public class LlmConfig
{
    public string? ApiKey { get; set; }
    public bool ApiKeyRequired { get; set; }
    public string? Url { get; set; }
    public string? Model { get; set; }
    public int? RetryCount { get; set; }
    public int? BatchSize { get; set; }
    public bool SkipLineValidation { get; set; }
    public bool CorrectionPromptsEnabled { get; set; }
    public bool TranslateFlagged { get; set; }
    public Dictionary<string, object>? ModelParams { get; set; }

    // Not serialised in Yaml
    [YamlIgnore]
    public Dictionary<string, string> Prompts { get; set; } = [];

    [YamlIgnore]
    public string? WorkingDirectory { get; set; }

    [YamlIgnore]
    public List<GlossaryLine> GlossaryLines { get; set; } = [];

    [YamlIgnore]
    public List<GlossaryLine> ManualTranslations { get; set; } = [];

    [YamlIgnore]
    public Dictionary<string, string> TranslationCache { get; set; } = [];
}

public static class Configuration
{
    public static LlmConfig GetConfiguration(string workingDirectory)
    {       
        var deserializer = Yaml.CreateDeserializer();
        var response = deserializer.Deserialize<LlmConfig>(File.ReadAllText($"{workingDirectory}/Config.yaml", Encoding.UTF8));

        response.WorkingDirectory = workingDirectory;
        response.Prompts = CachePrompts(workingDirectory);
        response.GlossaryLines = deserializer.Deserialize<List<GlossaryLine>>(File.ReadAllText($"{workingDirectory}/Glossary.yaml", Encoding.UTF8)) ?? [];
        response.ManualTranslations = deserializer.Deserialize<List<GlossaryLine>>(File.ReadAllText($"{workingDirectory}/ManualTranslations.yaml", Encoding.UTF8)) ?? [];

        foreach(var line in response.GlossaryLines)
            line.Result = line.Result.Replace("-", "\u2011"); //Change Hyphens to non breaking hyphens

        return response;
    }

    public static Dictionary<string, string> CachePrompts(string workingDirectory)
    {
        var prompts = new Dictionary<string, string>();
        var path = $"{workingDirectory}/Prompts";

        foreach (var file in Directory.EnumerateFiles(path))
            prompts.Add(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file));

        return prompts;
    }   

    //public static void AddToDictionaryGlossary(Dictionary<string, string> globalGlossary, List<DataLine> entries)
    //{
    //    foreach (var line in entries)
    //        globalGlossary.Add(line.Raw, line.Result);
    //}  
}
