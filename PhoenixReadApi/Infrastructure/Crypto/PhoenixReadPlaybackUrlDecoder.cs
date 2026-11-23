using System.Security.Cryptography;
using System.Text;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>解码或解密短剧播放地址（支持明文、Base64 以及 key_seed AES-128-CBC 封包）。</summary>
internal static class PhoenixReadPlaybackUrlDecoder
{
    private static readonly byte[] SeedConstants =
    [
        0x4d,
        0xd4,
        0xc2,
        0xe6,
        0xb8,
        0x31,
        0x62,
        0x09,
        0x0e,
        0x52,
        0xb3,
        0xc7,
        0xa6,
        0x73,
        0x3b,
        0xa4,
        0x1c,
        0xb2,
        0x46,
        0x2b,
        0x82,
        0x9a,
        0xb5,
        0x8a,
        0x19,
        0x6b,
        0x39,
        0xdb,
        0x57,
        0x17,
        0x75,
        0x24,
        0xf4,
        0x9b,
        0xaf,
        0x7f,
        0x08,
        0xe8,
        0xd6,
        0x8d,
        0x26,
        0xa7,
        0x2e,
        0x37,
        0xc1,
        0xa9,
        0x5a,
        0x2f,
        0x1f,
        0x05,
        0xa5,
        0x18,
        0x92,
        0xae,
        0xf2,
        0x94,
        0x97,
        0x32,
        0xb6,
        0x2a,
        0x38,
        0xaa,
        0xdd,
        0x58,
    ];

    internal static string Decode(string? value, string? keySeed)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();
        if (
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        )
        {
            return trimmed;
        }

        byte[] raw;
        try
        {
            raw = Base64Decoder.Decode(trimmed);
        }
        catch (FormatException)
        {
            return string.Empty;
        }

        if (raw.Length == 0)
        {
            return string.Empty;
        }

        if (raw[0] == 0xa8)
        {
            if (raw.Length < 20 || raw[2] != 1 || raw[3] != 0 || (raw.Length - 4) % 16 != 0 || string.IsNullOrWhiteSpace(keySeed))
            {
                return string.Empty;
            }

            byte[] seedBytes;
            try
            {
                seedBytes = Base64Decoder.Decode(keySeed.Trim());
            }
            catch (FormatException)
            {
                return string.Empty;
            }

            if (seedBytes.Length == 0)
            {
                return string.Empty;
            }

            Span<byte> first = stackalloc byte[64];
            SHA512.HashData(seedBytes, first);

            Span<byte> toHash = stackalloc byte[64 + 64];
            first.CopyTo(toHash);
            SeedConstants.CopyTo(toHash[64..]);

            Span<byte> keyHash = stackalloc byte[64];
            SHA512.HashData(toHash, keyHash);

            byte[] plaintext = new byte[raw.Length - 4];
            using (Aes aes = Aes.Create())
            {
                aes.Key = [.. keyHash[..16]];
                aes.IV = [.. keyHash[16..32]];
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                using ICryptoTransform decryptor = aes.CreateDecryptor();
                decryptor.TransformBlock(raw, 4, raw.Length - 4, plaintext, 0);
            }

            int padding = plaintext[^1];
            if (padding is > 0 and <= 16)
            {
                raw = plaintext[..^padding];
            }
            else
            {
                raw = plaintext;
            }
        }

        string decoded = Encoding.UTF8.GetString(raw).Trim('\0', ' ', '\r', '\n');
        if (
            decoded.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || decoded.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        )
        {
            return decoded;
        }

        return string.Empty;
    }
}
