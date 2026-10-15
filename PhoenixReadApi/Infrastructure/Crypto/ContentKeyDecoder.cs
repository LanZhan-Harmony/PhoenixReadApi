using System.Numerics;
using System.Text;

namespace PhoenixReadApi;

/// <summary>Decodes Phoenix CENC content keys at the crypto boundary.</summary>
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
        byte[] array2 = new byte[num];
        for (int i = 0; i < num; i++)
        {
            array2[i] = (byte)(array[array.Length - num + i] ^ b);
        }
        string text = Encoding.UTF8.GetString(array2);
        if (text == "app_v2" || text == "web_v2")
        {
            throw new PhoenixReadApiMediaException("媒体密钥版本暂不支持。", "media");
        }
        byte[] array3 = new byte[num2];
        byte b2 = 250;
        byte b3 = 85;
        for (int j = 0; j < num2; j++)
        {
            byte b4 = array[j + 1];
            byte b5 = b2;
            if (j % 2 == 0)
            {
                b2 = b4;
            }
            else
            {
                b5 = b3;
                b3 = b4;
            }
            array3[j] = (byte)((b5 ^ b4) - 21 - BitOperations.PopCount((uint)j));
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
        if (c >= '0' && c <= '9')
        {
            return c - 48;
        }
        if (c >= 'a' && c <= 'z')
        {
            return c - 97 + 10;
        }
        if (c >= 'A' && c <= 'Z')
        {
            return c - 65 + 10;
        }
        return -1;
    }
}

