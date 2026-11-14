using System.Numerics;
using System.Text;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>在加密处理层解码红果 CENC 内容密钥。</summary>
internal static class ContentKeyDecoder
{
    internal static byte[] Decode(string value)
    {
        if (value.Length > 1024)
        {
            throw new PhoenixReadApiResponseException("媒体密钥数据过长。", "media");
        }
        byte[] array;
        try
        {
            array = Base64Decoder.Decode(value);
        }
        catch (FormatException innerException)
        {
            throw new PhoenixReadApiResponseException(
                "媒体密钥编码无效。",
                "media",
                innerException
            );
        }
        if (array.Length < 3)
        {
            throw new PhoenixReadApiResponseException("媒体密钥结构无效。", "media");
        }
        int num = (array[0] ^ array[1] ^ array[2]) - 48;
        int num2 = array.Length - num - 1;
        if (num < 1 || num2 < 33 || num2 >= array.Length || array.Length - num - 2 < 0)
        {
            throw new PhoenixReadApiResponseException("媒体密钥结构无效。", "media");
        }
        byte b = (byte)(array[array.Length - num - 2] ^ array[array.Length - num - 1]);
        Span<byte> tagSpan = num <= 256 ? stackalloc byte[num] : new byte[num];
        for (int i = 0; i < num; i++)
        {
            tagSpan[i] = (byte)(array[array.Length - num + i] ^ b);
        }
        bool isV2 = tagSpan.StartsWith("app_v2"u8) || tagSpan.StartsWith("web_v2"u8);

        byte[] array3 = new byte[num2];
        Array.Copy(array, 1, array3, 0, num2);

        if (isV2)
        {
            int last = array3.Length - 1;
            for (int i = 0; i + 1 < array3.Length; i += 2)
            {
                (array3[i], array3[i + 1]) = (array3[i + 1], array3[i]);
                if (i == last - 2)
                {
                    break;
                }
            }
        }

        byte cur = 0xfa;
        byte prev = 0x55;
        for (int j = 0; j < num2; j++)
        {
            byte b4 = array3[j];
            byte term = (byte)-(0x15 + BitOperations.PopCount((uint)j));
            if (j % 2 == 0)
            {
                array3[j] = isV2 ? (byte)((cur ^ b4) - prev + term) : (byte)(term + (cur ^ b4));
                cur = b4;
            }
            else
            {
                array3[j] = isV2 ? (byte)((prev ^ b4) - prev + term) : (byte)(term + (prev ^ b4));
                prev = b4;
            }
        }

        int num3 = ParseBase36(array3[0]);
        if (num3 < 0 || num2 - num3 - 1 != 32)
        {
            throw new PhoenixReadApiMediaException("媒体密钥内容无效。", "media");
        }
        string s = Encoding.ASCII.GetString(array3, 1, 32);
        byte[] array4;
        try
        {
            array4 = Convert.FromHexString(s);
        }
        catch (FormatException innerException2)
        {
            throw new PhoenixReadApiMediaException(
                "媒体密钥不是有效的 AES-128 密钥。",
                "media",
                innerException2
            );
        }
        if (array4.Length != 16)
        {
            throw new PhoenixReadApiMediaException("媒体密钥不是有效的 AES-128 密钥。", "media");
        }
        return array4;
    }

    private static int ParseBase36(byte value)
    {
        char c = (char)value;
        return c switch
        {
            >= '0' and <= '9' => c - 48,
            >= 'a' and <= 'z' => c - 97 + 10,
            >= 'A' and <= 'Z' => c - 65 + 10,
            _ => -1,
        };
    }
}
