using System.IO.Compression;
using System.Security.Cryptography;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>ByteDance tt-data 加密封装工具。</summary>
internal static class TtEncrypt
{
    private static readonly byte[] TtEncryptConstant = Convert.FromHexString(
        "4dd4c2e6b83162090e52b3c7a6733ba41cb2462b829ab58a196b39db57177524f49baf7f08e8d68d26a72e37c1a95a2f1f05a51892aef2949732b62a38aadd58"
    );

    private static readonly byte[] TtEncryptPrefix = Convert.FromHexString(
        "3176c9b6d71579d7e82179c9e7cbde787b54d541bad3b15f2e8fed92293fa25a"
    );

    private static readonly byte[] KeyIv = ComputeKeyIv();

    private static byte[] ComputeKeyIv()
    {
        byte[] prefixHash = SHA512.HashData(TtEncryptPrefix);
        byte[] combined = new byte[prefixHash.Length + TtEncryptConstant.Length];
        prefixHash.CopyTo(combined, 0);
        TtEncryptConstant.CopyTo(combined, prefixHash.Length);
        return SHA512.HashData(combined);
    }

    /// <summary>将输入数据包装为 tt-data 封套并执行 AES-128-CBC 加密。</summary>
    internal static byte[] Encrypt(ReadOnlySpan<byte> input)
    {
        byte[] digest = SHA512.HashData(input);
        byte[] plain = new byte[digest.Length + input.Length];
        digest.CopyTo(plain, 0);
        input.CopyTo(plain.AsSpan(digest.Length));

        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = KeyIv[..16];
        aes.IV = KeyIv[16..32];

        using ICryptoTransform encrypter = aes.CreateEncryptor();
        byte[] encrypted = encrypter.TransformFinalBlock(plain, 0, plain.Length);

        byte[] output = new byte[6 + TtEncryptPrefix.Length + encrypted.Length];
        output[0] = 0x74;
        output[1] = 0x63;
        output[2] = 0x05;
        output[3] = 0x10;
        output[4] = 0x00;
        output[5] = 0x00;
        TtEncryptPrefix.CopyTo(output, 6);
        encrypted.CopyTo(output, 6 + TtEncryptPrefix.Length);
        return output;
    }

    /// <summary>使用 gzip 压缩输入后进行 tt-data 加密。</summary>
    internal static byte[] GzipAndEncrypt(ReadOnlySpan<byte> input)
    {
        using MemoryStream ms = new();
        using (GZipStream gzip = new(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(input);
        }

        return Encrypt(ms.ToArray());
    }
}
