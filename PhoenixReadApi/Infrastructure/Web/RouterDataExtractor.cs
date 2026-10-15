using System.Text.Json;
using System.Text.RegularExpressions;

namespace PhoenixReadApi.Internal.Infrastructure.Web;

/// <summary>Extracts the JSON object assigned to a Phoenix page's router data variable.</summary>
internal static partial class RouterDataExtractor
{
    internal static JsonDocument? Parse(string html)
    {
        Match match = RouterAssignmentRegex().Match(html);
        if (!match.Success)
        {
            return null;
        }

        var start = html.IndexOf('{', match.Index + match.Length);
        if (start < 0)
        {
            return null;
        }

        var end = FindJsonEnd(html, start);
        if (end <= start)
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(html.AsMemory(start, end - start));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static int FindJsonEnd(string value, int start)
    {
        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = start; index < value.Length; index++)
        {
            var current = value[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
            }
            else if (current is '{' or '[')
            {
                depth++;
            }
            else if (current is '}' or ']')
            {
                depth--;
                if (depth == 0)
                {
                    return index + 1;
                }
            }
        }

        return -1;
    }

    [GeneratedRegex("(?s)(?:window\\.)?_ROUTER_DATA\\s*=\\s*")]
    private static partial Regex RouterAssignmentRegex();
}
