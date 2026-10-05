using FanslationStudio.LlmKit.Support;
using Translate;

namespace Tests;

public class CharacterLineContextTests
{
    private static TranslationLine Line(string text) => new() { Raw = text, Splits = [new TranslationSplit { Text = text }] };

    [Fact(DisplayName = "CharacterGenders.yaml loads and covers the biography characters and the male player 赵活")]
    public void Table_Loads()
    {
        var characters = CharacterLineContext.Load(GameFileHandling.WorkingDirectory);

        Assert.Equal(LineContext.Female, characters["虞小梅"]);
        Assert.Equal(LineContext.Female, characters["小梅"]);
        Assert.Equal(LineContext.Male, characters["叶云舟"]);
        Assert.Equal(LineContext.Male, characters["云舟"]);
        Assert.Equal(LineContext.Male, characters["赵活"]);
        // A character whose gender is still empty in the table is left out rather than guessed.
        Assert.False(characters.ContainsKey("向无忧"));
    }

    [Fact(DisplayName = "CharacterLineContext gives a line naming a listed character that character's gender")]
    public void Provide_NamedCharacter()
    {
        var xiaomei = Line("小梅与有荣焉，没少夸你，惹得众人羡慕。");
        var player = Line("冒牌赵活被抓住了。");
        var unlisted = Line("向无忧被抓住了。");
        var lines = new List<TranslationLine> { xiaomei, player, unlisted };

        var contexts = CharacterLineContext.Provide(GameFileHandling.WorkingDirectory, new TextFileToSplit { Path = "Legend_01_zh-cn.csv" }, lines);

        Assert.Equal(LineContext.Female, contexts[xiaomei.Splits[0]].Gender);
        Assert.True(contexts[xiaomei.Splits[0]].GenderKnown);
        Assert.Equal(LineContext.Male, contexts[player.Splits[0]].Gender);
        Assert.False(contexts.ContainsKey(unlisted.Splits[0]));
    }
}
