using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PhoenixReadApi.Internal.Serialization;

/// <summary>Reads scalar and collection values at a provider contract boundary.</summary>
internal static partial class JsonValueReader
{
    private static readonly char[] Separators = [',', '/', '，', '、'];

    internal static bool IsNumericId(string? value) =>
        value is not null && NumericIdRegex().IsMatch(value);

    internal static string String(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out JsonElement value))
            {
                continue;
            }

            var result = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => string.Empty,
            };
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result.Trim();
            }
        }

        return string.Empty;
    }

    internal static string String(JsonElement? element, params string[] names) =>
        element is { } value ? String(value, names) : string.Empty;

    internal static int Int(JsonElement element, params string[] names) =>
        int.TryParse(
            String(element, names),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : 0;

    internal static long Long(JsonElement element, params string[] names) =>
        long.TryParse(
            String(element, names),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : 0;

    internal static double Double(JsonElement element, params string[] names) =>
        double.TryParse(
            String(element, names),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : 0;

    internal static bool Bool(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out JsonElement value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (bool.TryParse(value.ToString(), out var result))
            {
                return result;
            }
        }

        return false;
    }

    internal static JsonElement Object(JsonElement element, params string[] path)
    {
        JsonElement current = element;
        foreach (var name in path)
        {
            if (
                current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(name, out current)
            )
            {
                return default;
            }
        }

        return current;
    }

    internal static IEnumerable<JsonElement> Array(JsonElement element, string name)
    {
        if (
            element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(name, out JsonElement value)
        )
        {
            return [];
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray();
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var fallback in new[] { "list", "items", "data" })
            {
                if (
                    value.TryGetProperty(fallback, out JsonElement nested)
                    && nested.ValueKind == JsonValueKind.Array
                )
                {
                    return nested.EnumerateArray();
                }
            }
        }

        return [];
    }

    internal static List<string> StringList(JsonElement element, params string[] names)
    {
        List<string> result = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (
                element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(name, out JsonElement value)
            )
            {
                Add(value);
            }
        }
        return result;

        void Add(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    foreach (
                        var item in value
                            .GetString()!
                            .Split(
                                Separators,
                                StringSplitOptions.RemoveEmptyEntries
                                    | StringSplitOptions.TrimEntries
                            )
                    )
                    {
                        if (seen.Add(item))
                        {
                            result.Add(item);
                        }
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (JsonElement item in value.EnumerateArray())
                    {
                        Add(item);
                    }

                    break;
                case JsonValueKind.Object:
                    var name = String(value, "name", "title", "label", "tag");
                    if (name.Length > 0 && seen.Add(name))
                    {
                        result.Add(name);
                    }

                    break;
                default:
                    var text = value.ToString().Trim();
                    if (text.Length > 0 && seen.Add(text))
                    {
                        result.Add(text);
                    }

                    break;
            }
        }
    }

    internal static string First(params string?[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim()
        ?? string.Empty;

    internal static int Quality(string name) =>
        int.TryParse(
            QualityNumberRegex().Match(name).Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : 0;

    [GeneratedRegex("^[0-9]{1,32}$")]
    private static partial Regex NumericIdRegex();

    [GeneratedRegex("[0-9]+")]
    private static partial Regex QualityNumberRegex();
}
