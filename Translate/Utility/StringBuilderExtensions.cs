using System.Text;
using System.Text.RegularExpressions;

namespace Translate.Utility;

public static class StringBuilderExtensions
{
    public static void Replace(this StringBuilder sb, Regex regex, MatchEvaluator evaluator)
    {
        var result = regex.Replace(sb.ToString(), evaluator);
        sb.Clear();
        sb.Append(result);
    }
}
