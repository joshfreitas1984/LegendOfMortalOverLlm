using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Translate;

public class TranslationSplit
{
    public int Split { get; set; } = 0;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Text { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Translated { get; set; } = string.Empty;

    public bool SafeToTranslate { get; set; } = true;

    public bool FlaggedForRetranslation { get; set; } = false;

    //public bool FlaggedForGlossaryExtraction { get; set; } = true;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string FlaggedMistranslation { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string FlaggedHallucination { get; set; } = string.Empty;

    //public DateTime LastTranslatedOn = DateTime.Now;

    public TranslationSplit() { }

    public TranslationSplit(int split, string text)
    {
        Split = split;
        Text = text;
    }

    public void ResetFlags(bool translated = true)
    {
        //if (translated)
        //    LastTranslatedOn = DateTime.Now;

        FlaggedForRetranslation = false;
        FlaggedMistranslation = string.Empty;
        FlaggedHallucination = string.Empty;
    }

    //public void ResetGlossaryFlags()
    //{
    //    FlaggedForGlossaryExtraction = true;
    //}
}
