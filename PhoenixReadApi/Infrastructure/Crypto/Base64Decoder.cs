using System.Buffers;

namespace PhoenixReadApi;

/// <summary>Decodes standard and URL-safe provider base64 values.</summary>
internal static class Base64Decoder
{
    internal static byte[] Decode(string value)
    {
        ReadOnlySpan<char> span = value.AsSpan().Trim();

        // 快速路径：标准 Base64，长度是 4 的倍数，且不含 URL-safe 字符
        if (span.Length % 4 == 0 && span.IndexOf('-') < 0 && span.IndexOf('_') < 0)
        {
            int padding = 0;
            if (span.Length > 0 && span[^1] == '=')
            {
                padding++;
            }
            if (span.Length > 1 && span[^2] == '=')
            {
                padding++;
            }

            int byteCount = span.Length / 4 * 3 - padding;
            byte[] result = new byte[byteCount];

            if (Convert.TryFromBase64Chars(span, result, out int written) && written == byteCount)
            {
                return result;
            }

            throw new FormatException("Invalid Base64 string.");
        }

        // Fallback：处理 Base64URL 和缺少 padding
        int len = span.Length;
        int mod = len % 4;
        int paddingNeeded = mod == 0 ? 0 : 4 - mod;
        int totalLen = len + paddingNeeded;

        if (totalLen <= 256)
        {
            Span<char> charBuffer = stackalloc char[totalLen];
            return DecodeWithBuffer(span, charBuffer, paddingNeeded);
        }
        else
        {
            char[] rented = ArrayPool<char>.Shared.Rent(totalLen);
            try
            {
                return DecodeWithBuffer(span, rented.AsSpan(0, totalLen), paddingNeeded);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    private static byte[] DecodeWithBuffer(
        ReadOnlySpan<char> span,
        Span<char> charBuffer,
        int paddingNeeded
    )
    {
        int len = span.Length;

        for (int i = 0; i < len; i++)
        {
            char c = span[i];
            charBuffer[i] = c switch
            {
                '-' => '+',
                '_' => '/',
                _ => c,
            };
        }

        for (int i = len; i < charBuffer.Length; i++)
        {
            charBuffer[i] = '=';
        }

        int byteCount = charBuffer.Length / 4 * 3 - paddingNeeded;
        byte[] result = new byte[byteCount];

        if (Convert.TryFromBase64Chars(charBuffer, result, out int written) && written == byteCount)
        {
            return result;
        }

        throw new FormatException("Invalid Base64 string.");
    }
}
