using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>解码可选的备用播放响应封装。</summary>
internal static class PlaybackResponseDecoder
{
    internal static byte[] Decode(string body)
    {
        string text = body.Trim();
        if (!text.StartsWith("v2.", StringComparison.Ordinal))
        {
            return Encoding.UTF8.GetBytes(text);
        }
        string[] array = text.Split('.', 3);
        if (array.Length != 3 || array[1].Length <= 4 || array[1].Length > 1028)
        {
            throw new PhoenixReadApiResponseException("备用播放接口响应密钥无效。", "playback");
        }
        byte[] array2;
        try
        {
            array2 = Convert.FromHexString(array[1].AsSpan(4));
        }
        catch (FormatException innerException)
        {
            throw new PhoenixReadApiResponseException(
                "备用播放接口响应密钥无效。",
                "playback",
                innerException
            );
        }
        if (array2.Length < 32)
        {
            throw new PhoenixReadApiResponseException("备用播放接口响应密钥无效。", "playback");
        }
        ReadOnlySpan<byte> readOnlySpan =
        [
            104,
            64,
            70,
            166,
            190,
            168,
            143,
            130,
            225,
            254,
            251,
            217,
            196,
            34,
            45,
            60,
            29,
            20,
            103,
            105,
        ];
        byte[] array3 = new byte[array2.Length];
        for (int i = 0; i < array2.Length; i++)
        {
            byte b = (byte)((i == 0) ? 109 : array2[i - 1]);
            int num = i % readOnlySpan.Length;
            byte b2 = (byte)(readOnlySpan[num] ^ (byte)(90 + 13 * num) ^ 0x55);
            byte value = (byte)(array2[i] + 215 - 11 * i);
            array3[i] = (byte)(b ^ b2 ^ RotateLeft(value, 3));
        }
        byte[] array4;
        try
        {
            array4 = Base64Decoder.Decode(array[2]);
        }
        catch (FormatException innerException2)
        {
            throw new PhoenixReadApiResponseException(
                "备用播放接口加密响应无效。",
                "playback",
                innerException2
            );
        }
        if (array4.Length == 0 || array4.Length % 16 != 0)
        {
            throw new PhoenixReadApiResponseException("备用播放接口加密响应无效。", "playback");
        }
        try
        {
            using Aes aes = Aes.Create();
            aes.Key = array3[..16];
            aes.IV = array3[16..32];
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes.CreateDecryptor().TransformFinalBlock(array4, 0, array4.Length);
        }
        catch (CryptographicException innerException3)
        {
            throw new PhoenixReadApiResponseException(
                "备用播放接口响应解密失败。",
                "playback",
                innerException3
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte RotateLeft(byte value, int offset)
    {
        return (byte)((value << offset) | (value >> 8 - offset));
    }
}
