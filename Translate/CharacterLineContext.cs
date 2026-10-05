using FanslationStudio.LlmKit.Support;

namespace Translate;

/// <summary>
/// Line context for Legend of Mortal: a line that names a character from Files/CharacterGenders.yaml (derived once from
/// the character biographies) is told that character's gender, so a pronoun for them is written and checked against it.
/// The player character 赵活 is listed as male (confirmed by the project owner). Used only when Config.yaml's
/// lineContextEnabled is true.
/// </summary>
public static class CharacterLineContext
{
    private const string TableFile = "CharacterGenders.yaml";

    private static readonly Lazy<IReadOnlyDictionary<string, string>> Characters = new(() => Load(GameFileHandling.WorkingDirectory));

    public static IReadOnlyDictionary<TranslationSplit, LineContext> Provide(string workingDirectory, TextFileToSplit textFile, IReadOnlyList<TranslationLine> lines)
    {
        // The cached table is for the game's own working directory; any other directory (a test copy) is read directly.
        var characters = string.Equals(workingDirectory, GameFileHandling.WorkingDirectory, StringComparison.Ordinal)
            ? Characters.Value
            : Load(workingDirectory);

        var contexts = new Dictionary<TranslationSplit, LineContext>();
        CharacterContext.AddCharacterContext(contexts, lines, characters);
        return contexts;
    }

    public static IReadOnlyDictionary<string, string> Load(string workingDirectory) =>
        CharacterContext.FromYaml(Path.Combine(workingDirectory, TableFile));
}
