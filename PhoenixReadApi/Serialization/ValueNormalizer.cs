using System.Globalization;

namespace PhoenixReadApi.Internal.Serialization;

/// <summary>规范化已反序列化的标量值、ID 和清晰度。</summary>
internal static class ValueNormalizer
{
    internal static bool IsNumericId(string? value)
    {
        if (value is null or { Length: 0 or > 32 })
        {
            return false;
        }

        foreach (char c in value)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }

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

    internal static int Quality(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return 0;
        }

        ReadOnlySpan<char> span = name.AsSpan();
        int start = -1;
        for (int i = 0; i < span.Length; i++)
        {
            if (char.IsAsciiDigit(span[i]))
            {
                if (start < 0)
                {
                    start = i;
                }
            }
            else if (start >= 0)
            {
                return int.TryParse(
                    span[start..i],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int val
                )
                    ? val
                    : 0;
            }
        }

        if (start >= 0)
        {
            return int.TryParse(
                span[start..],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int val
            )
                ? val
                : 0;
        }

        return 0;
    }

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

        ReadOnlySpan<char> span = rawCodec.AsSpan().Trim();
        if (
            span.Equals("h264", StringComparison.OrdinalIgnoreCase)
            || span.Equals("avc1", StringComparison.OrdinalIgnoreCase)
            || span.Equals("avc", StringComparison.OrdinalIgnoreCase)
        )
        {
            return MediaCodec.H264;
        }

        if (
            span.Equals("bytevc1", StringComparison.OrdinalIgnoreCase)
            || span.Equals("h265", StringComparison.OrdinalIgnoreCase)
            || span.Equals("hevc", StringComparison.OrdinalIgnoreCase)
        )
        {
            return MediaCodec.ByteVC1;
        }

        if (
            span.Equals("bytevc2", StringComparison.OrdinalIgnoreCase)
            || span.Equals("h266", StringComparison.OrdinalIgnoreCase)
            || span.Equals("vvc", StringComparison.OrdinalIgnoreCase)
        )
        {
            return MediaCodec.ByteVC2;
        }

        return MediaCodec.Unknown;
    }

    internal static string FormatCount(long count)
    {
        if (count < 0)
        {
            return "0";
        }

        if (count >= 100_000_000)
        {
            return (count / 100_000_000.0).ToString("0.#", CultureInfo.InvariantCulture) + "亿";
        }

        if (count >= 10_000)
        {
            return (count / 10_000.0).ToString("0.#", CultureInfo.InvariantCulture) + "万";
        }

        return count.ToString(CultureInfo.InvariantCulture);
    }
}
