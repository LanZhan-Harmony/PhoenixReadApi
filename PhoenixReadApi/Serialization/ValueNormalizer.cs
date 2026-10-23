using System.Globalization;
using System.Text.RegularExpressions;

namespace PhoenixReadApi.Internal.Serialization;

/// <summary>规范化已反序列化的标量值、ID 和清晰度。</summary>
internal static partial class ValueNormalizer
{
    internal static bool IsNumericId(string? value) =>
        value is not null && NumericIdRegex().IsMatch(value);

    internal static string First(params string?[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim()
        ?? string.Empty;

    internal static int Quality(string name) =>
        int.TryParse(
            QualityNumberRegex().Match(name).Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int value
        )
            ? value
            : 0;

    [GeneratedRegex("^[0-9]{1,32}$")]
    private static partial Regex NumericIdRegex();

    [GeneratedRegex("[0-9]+")]
    private static partial Regex QualityNumberRegex();
}
