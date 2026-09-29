using Translate;

namespace Tests;

public class SoftLineBreakJoinerTests
{
    [Theory]
    // Break after '，' is a soft wrap - joined; break after '。' is kept.
    [InlineData("唐门弟子人手一柄的标准配备。\\n外型看似与脱手标相去不远，\\n实则是柄机关剑。",
        "Standard issue for Tang disciples.\nIt looks much like a throwing dart,\nbut is in fact a mechanism sword.",
        "Standard issue for Tang disciples.\nIt looks much like a throwing dart, but is in fact a mechanism sword.")]
    // Literal "\n" in the translation is preserved as-is where kept.
    [InlineData("捅死你不麻烦，\\n麻烦的是等下要怎么处理你的尸体。\\n再见。",
        "Killing you is easy,\\nthe hard part is the body.\\nBye.",
        "Killing you is easy, the hard part is the body.\\nBye.")]
    // '、' and '；' are soft too; trailing/leading whitespace around the join is collapsed.
    [InlineData("造型奇特、\\n却极其锋利；\\n好刀。",
        "Strangely shaped \nyet extremely sharp;\n Fine blade.",
        "Strangely shaped yet extremely sharp; Fine blade.")]
    // Headers ('：'), [tags] and leading breaks are kept.
    [InlineData("嘴攻：\\n出口伤人。", "Verbal Attack:\nWounding words.", "Verbal Attack:\nWounding words.")]
    [InlineData("[决斗]\\n提升伤害减免。", "[Duel]\nIncreases damage reduction.", "[Duel]\nIncreases damage reduction.")]
    [InlineData("\\n你独行至后山，\\n眺望远山。", "\nYou walk alone,\ngazing at the mountains.", "\nYou walk alone, gazing at the mountains.")]
    // Break-count mismatch between source and translation - left untouched.
    [InlineData("一，\\n二，\\n三。", "One, two,\nthree.", "One, two,\nthree.")]
    // No breaks at all.
    [InlineData("没有换行。", "No line breaks.", "No line breaks.")]
    public void JoinField_JoinsOnlySoftBreaks(string source, string translated, string expected)
    {
        Assert.Equal(expected, SoftLineBreakJoiner.JoinField(source, translated));
    }

    [Fact]
    public void Apply_LeavesKeyColumnAndUnmatchedColumnsAlone()
    {
        var source = new[] { "Equip/Desc/Melee_11", "造型奇特、\\n却极其锋利。" };
        var translated = new[] { "Equip/Desc/Melee_11", "Strangely shaped\nyet extremely sharp." };

        var result = SoftLineBreakJoiner.Apply(source, translated);

        Assert.Equal(["Equip/Desc/Melee_11", "Strangely shaped yet extremely sharp."], result);
    }
}
