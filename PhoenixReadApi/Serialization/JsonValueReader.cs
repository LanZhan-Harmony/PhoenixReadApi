using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PhoenixReadApi.Internal.Serialization;

/// <summary>在红果接口契约层读取标量值和集合值。</summary>
internal static partial class JsonValueReader
{
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

    internal static long Long(JsonElement element, params string[] names) =>
        long.TryParse(
            String(element, names),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : 0;

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
