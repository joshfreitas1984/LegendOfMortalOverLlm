using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace SharedAssembly.DynamicStrings;

//Keep in sync with patch and translate
public class DynamicStringContract
{
    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Type { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Method { get; set; } = string.Empty;

    public long ILOffset { get; set; } = 0;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Raw { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Translation { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string[] Parameters { get; set; } = [];
}

public class GroupedDynamicStringContracts
{
    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Type { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Method { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string[] Parameters { get; set; } = [];

    public DynamicStringContract[] Contracts { get; set; } = [];
}