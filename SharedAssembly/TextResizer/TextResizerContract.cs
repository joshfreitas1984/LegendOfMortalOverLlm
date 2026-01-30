using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace SharedAssembly.TextResizer;

public record TextResizerContract
{
    // Text Component Details

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Path = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string SampleText = string.Empty;

    public float? IdealFontSize;

    public float? FontPercentage;

    public float? MinFontSize;

    public float? MaxFontSize;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Alignment = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string OverflowMode = string.Empty;

    public bool? AllowWordWrap;

    public bool? AllowAutoSizing;

    // Custom Flags

    // To deal with some of the wierdness in the game
    public bool AllowLeftTrimText = false;

    public float AdjustX;

    public float AdjustY;

    public float AdjustWidth;

    public float AdjustHeight;

    public float? LineSpacing;

    public float? CharacterSpacing;

    public float? WordSpacing;

    //[YamlIgnore]
    //public Regex? CompiledRegex { get; private set; }

    //public TextResizerContract()
    //{
    //    if (Path.Contains("*"))
    //    {
    //        // Convert to Regex
    //        var pattern = Path
    //            .Replace("/", @"\/")
    //            .Replace("(", @"\(")
    //            .Replace(")", @"\)")
    //            .Replace("*", ".*");

    //        CompiledRegex = new Regex(pattern, RegexOptions.Compiled);
    //    }
    //}

    public TextResizerContract ShallowClone()
    {
        return (TextResizerContract)MemberwiseClone();
    }
}
