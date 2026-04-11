using System.Text.RegularExpressions;

namespace Translate.Utility;

public record TagValidationResult(bool IsValid, HashSet<string> MissingTags, HashSet<string> ExtraTags);

public static class HtmlTagHelpers
{
    public static TagValidationResult ValidateTags(string raw, string translated, bool allowMissingColors)
    {
        HashSet<string> rawTags = ExtractTagsWithAttributes(raw, true);
        HashSet<string> translatedTags = ExtractTagsWithAttributes(translated, false);

        var response = rawTags.SetEquals(translatedTags);
        var missingTags = new HashSet<string>();
        var extraTags = new HashSet<string>();

        if (!response
            && allowMissingColors
            && rawTags.Count != translatedTags.Count) //So if its got them get it right
        {
            rawTags.RemoveWhere(tag => tag.Contains("color"));
            translatedTags.RemoveWhere(tag => tag.Contains("color"));
            response = rawTags.SetEquals(translatedTags);
        }

        // Test for Trimmed tags (when we're using the raw in the validation test)
        if (!response)
        {
            var trimmedTags = new HashSet<string>();
            foreach (var tag in rawTags)
                trimmedTags.Add(tag.Trim());

            response = trimmedTags.SetEquals(translatedTags);

            if (!response)
            {
                // Calculate missing and extra tags
                missingTags = new HashSet<string>(trimmedTags.Except(translatedTags));
                extraTags = new HashSet<string>(translatedTags.Except(trimmedTags));
            }
        }
        else if (!response)
        {
            // Calculate missing and extra tags without trimming
            missingTags = new HashSet<string>(rawTags.Except(translatedTags));
            extraTags = new HashSet<string>(translatedTags.Except(rawTags));
        }

        return new TagValidationResult(response, missingTags, extraTags);
    }

    private static HashSet<string> ExtractTagsWithAttributes(string input, bool updateSizes)
    {
        var tags = new HashSet<string>();
        var regex = new Regex(@"<(/?\w+\s*[^>]*)>");
        foreach (Match match in regex.Matches(input))
        {
            var tag = match.Groups[1].Value;

            // Update size tags if needed
            if (updateSizes && tag.StartsWith("size"))
            {
                var newSize = StringTokenReplacer.CalculateNewSize($"<{tag}>");
                tag = tag.Contains("#") ? $"size=#{newSize}" : $"size={newSize}";
            }

            tags.Add(tag);
        }
        return tags;
    }

    public static List<string> ExtractTagsListWithAttributes(string input, params string[] ignore)
    {
        var tags = new List<string>();
        var regex = new Regex(@"<(\w+\s*[^/>]*)>");
        foreach (Match match in regex.Matches(input))
        {
            var tagValue = match.Groups[1].Value;
            if (!ignore.Any(i => tagValue.StartsWith(i)))
                tags.Add($"<{tagValue}>");
        }
        return tags;
    }

    public static string TrimHtmlTagsInContent(string input)
    {
        // Regular expression to match HTML tags and remove extra spaces, including self-closing tags
        var tagPattern = new Regex(@"<\s*(\w+)(.*?)\s*/?>");

        // Replace each tag by trimming unnecessary spaces inside the tag
        return tagPattern.Replace(input, match =>
        {
            var tagName = match.Groups[1].Value;
            var attributes = match.Groups[2].Value.Trim();

            // Determine if the tag is self-closing
            bool isSelfClosing = match.Value.EndsWith("/>");

            // Rebuild the tag with no extra spaces and ensure self-closing tag has the slash without spaces before it
            return isSelfClosing
                ? $"<{tagName}{(string.IsNullOrEmpty(attributes) ? "" : " " + attributes)}/>"
                : $"<{tagName}{(string.IsNullOrEmpty(attributes) ? "" : " " + attributes)}>";
        });
    }
}

