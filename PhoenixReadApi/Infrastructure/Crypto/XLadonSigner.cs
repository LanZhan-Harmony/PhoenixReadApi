using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>
/// 六神算法之 X-Ladon 签名器：34 轮 ARX 迭代与消息认证码生成。
/// </summary>
internal static class XLadonSigner
{
    private const string PlaybackLicenseId = "1588093228";
    private const string PlayerLicenseId = "1611921764";

    /// <summary>为播放或 video_tab 请求生成 X-Ladon 签名。</summary>
    public static string SignPlayback(uint timestamp, uint nonce, string aid = "8662")
    {
        Span<byte> seed = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(seed, nonce);

        int aidByteCount = Encoding.UTF8.GetByteCount(aid);
        Span<byte> digestInput = stackalloc byte[4 + aidByteCount];
        seed.CopyTo(digestInput);
        Encoding.UTF8.GetBytes(aid, digestInput[4..]);

        Span<byte> md5Digest = stackalloc byte[16];
        MD5.HashData(digestInput, md5Digest);

        Span<byte> hexKey = stackalloc byte[32];
        FormatHexBytesLower(md5Digest, hexKey);

        Span<ulong> keys = stackalloc ulong[34];
        ulong k = BinaryPrimitives.ReadUInt64LittleEndian(hexKey);
        Span<ulong> pending =
        [
            BinaryPrimitives.ReadUInt64LittleEndian(hexKey[8..]),
            BinaryPrimitives.ReadUInt64LittleEndian(hexKey[16..]),
            BinaryPrimitives.ReadUInt64LittleEndian(hexKey[24..]),
        ];

        for (int i = 0; i < 34; i++)
        {
            keys[i] = k;
            ulong x = (BitOperations.RotateRight(pending[0], 8) + k) ^ (ulong)i;
            pending[0] = pending[1];
            pending[1] = pending[2];
            pending[2] = x;
            k = x ^ BitOperations.RotateLeft(k, 3);
        }

        string rawText =
            $"{timestamp.ToString(CultureInfo.InvariantCulture)}-{PlaybackLicenseId}-{aid}";
        int rawByteCount = Encoding.UTF8.GetByteCount(rawText);
        Span<byte> rawBytes = stackalloc byte[rawByteCount];
        Encoding.UTF8.GetBytes(rawText, rawBytes);
        byte[] plain = Pad(rawBytes);

        for (int at = 0; at < plain.Length; at += 16)
        {
            ulong a = BinaryPrimitives.ReadUInt64LittleEndian(plain.AsSpan(at));
            ulong b = BinaryPrimitives.ReadUInt64LittleEndian(plain.AsSpan(at + 8));
            foreach (ulong rk in keys)
            {
                b = rk ^ (a + BitOperations.RotateRight(b, 8));
                a = b ^ BitOperations.RotateLeft(a, 3);
            }
            BinaryPrimitives.WriteUInt64LittleEndian(plain.AsSpan(at), a);
            BinaryPrimitives.WriteUInt64LittleEndian(plain.AsSpan(at + 8), b);
        }

        byte[] result = new byte[4 + plain.Length];
        seed.CopyTo(result);
        plain.CopyTo(result.AsSpan(4));
        return Convert.ToBase64String(result);
    }

    /// <summary>为客户端 Player 协议请求生成 X-Ladon 签名。</summary>
    public static string SignPlayer(string aid, uint timestamp, ReadOnlySpan<byte> prefix)
    {
        int aidBytesCount = Encoding.UTF8.GetByteCount(aid);
        Span<byte> input = stackalloc byte[prefix.Length + aidBytesCount];
        prefix.CopyTo(input);
        Encoding.UTF8.GetBytes(aid, input[prefix.Length..]);
        Span<byte> md5 = stackalloc byte[16];
        MD5.HashData(input, md5);
        Span<byte> bytes = stackalloc byte[32];
        FormatHexBytesLower(md5, bytes);

        ulong num = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(bytes[8..]);
        Queue<ulong> queue = new([
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[16..]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[24..]),
        ]);
        ulong[] array2 = new ulong[34];
        for (int i = 0; i < array2.Length; i++)
        {
            array2[i] = num;
            ulong num2 = (BitOperations.RotateRight(value, 8) + num) ^ (ulong)i;
            queue.Enqueue(num2);
            num = num2 ^ BitOperations.RotateLeft(num, 3);
            value = queue.Dequeue();
        }

        byte[] array3 = Pad(
            Encoding.UTF8.GetBytes(
                timestamp.ToString(CultureInfo.InvariantCulture) + "-" + PlayerLicenseId + "-" + aid
            )
        );
        for (int j = 0; j < array3.Length; j += 16)
        {
            num = BinaryPrimitives.ReadUInt64LittleEndian(array3.AsSpan(j));
            value = BinaryPrimitives.ReadUInt64LittleEndian(array3.AsSpan(j + 8));
            foreach (ulong num3 in array2)
            {
                value = num3 ^ (num + BitOperations.RotateRight(value, 8));
                num = value ^ BitOperations.RotateLeft(num, 3);
            }
            BinaryPrimitives.WriteUInt64LittleEndian(array3.AsSpan(j), num);
            BinaryPrimitives.WriteUInt64LittleEndian(array3.AsSpan(j + 8), value);
        }
        byte[] array5 = new byte[prefix.Length + array3.Length];
        prefix.CopyTo(array5);
        array3.CopyTo(array5.AsSpan(prefix.Length));
        return Convert.ToBase64String(array5);
    }

    private static byte[] Pad(ReadOnlySpan<byte> data)
    {
        int pad = 16 - (data.Length % 16);
        byte[] result = new byte[data.Length + pad];
        data.CopyTo(result);
        result.AsSpan(data.Length).Fill((byte)pad);
        return result;
    }

    private static void FormatHexBytesLower(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        for (int i = 0; i < source.Length; i++)
        {
            byte b = source[i];
            destination[i * 2] = GetHexCharLower(b >> 4);
            destination[i * 2 + 1] = GetHexCharLower(b & 0xF);
        }
    }

    private static byte GetHexCharLower(int nibble) =>
        (byte)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));
}
