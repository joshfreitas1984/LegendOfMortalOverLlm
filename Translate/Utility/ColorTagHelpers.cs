using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Translate.Utility;

public static class ColorTagHelpers
{
    public static bool StartsWithHalfColorTag(string input, out string start, out string end)
    {
        start = string.Empty;
        end = string.Empty;

        // Define the regex pattern
        string fullMatchPattern = @"^<color=[^>]+>(?!.*<\/color>)(.*)$";
        var groupPattern = @"(<color=[^>]+>)(?!.*<\/color>)(.*)";

        // Perform the match
        var isMatch = Regex.IsMatch(input, fullMatchPattern);

        if (isMatch)
        {
            var match = Regex.Match(input, groupPattern);
            // If regex matches, set start and end
            start = match.Groups[1].Value; // Full <color> tag
            end = match.Groups[2].Value;   // Content after the <color> tag
        }
        
        return isMatch;
    }
}
