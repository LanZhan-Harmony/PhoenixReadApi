using System.Globalization;
using System.Text.RegularExpressions;

namespace PhoenixReadApi.Internal.Serialization;

/// <summary>规范化已反序列化的标量值、ID 和清晰度。</summary>
internal static partial class ValueNormalizer
{
    internal static bool IsNumericId(string? value) =>
        value is not null && NumericIdRegex().IsMatch(value);

    internal static string First(params ReadOnlySpan<string?> values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    internal static int Quality(string name) =>
        int.TryParse(
            QualityNumberRegex().Match(name).Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int value
        )
            ? value
            : 0;

    internal static MediaDefinition ParseDefinition(int height, string? rawDefinition)
    {
        int val = height;
        if (val <= 0 && !string.IsNullOrWhiteSpace(rawDefinition))
        {
            val = Quality(rawDefinition);
        }

        return val switch
        {
            360 => MediaDefinition.P360,
            480 => MediaDefinition.P480,
            540 => MediaDefinition.P540,
            720 => MediaDefinition.P720,
            1080 => MediaDefinition.P1080,
            2160 => MediaDefinition.P2160,
            _ => MediaDefinition.Unknown,
        };
    }

    internal static MediaCodec ParseCodec(string? rawCodec)
    {
        if (string.IsNullOrWhiteSpace(rawCodec))
        {
            return MediaCodec.Unknown;
        }

        string lower = rawCodec.Trim().ToLowerInvariant();
        return lower switch
        {
            "h264" or "avc1" or "avc" => MediaCodec.H264,
            "bytevc1" or "h265" or "hevc" => MediaCodec.ByteVC1,
            "bytevc2" or "h266" or "vvc" => MediaCodec.ByteVC2,
            _ => MediaCodec.Unknown,
        };
    }

    [GeneratedRegex("^[0-9]{1,32}$")]
    private static partial Regex NumericIdRegex();

    [GeneratedRegex("[0-9]+")]
    private static partial Regex QualityNumberRegex();
}
