using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>
/// 六神算法之 X-Gorgon 签名器：基于 RC4 与位反转置换的请求签名算法。
/// </summary>
internal static class XGorgonSigner
{
    private static readonly byte[] RequestKey =
    [
        0xDF,
        0x77,
        0xB9,
        0x40,
        0xB9,
        0x9B,
        0x84,
        0x83,
        0xD1,
        0xB9,
        0xCB,
        0xD1,
        0xF7,
        0xC2,
        0xB9,
        0x85,
        0xC3,
        0xD0,
        0xFB,
        0xC3,
    ];

    /// <summary>为常规 App GET/POST 请求生成 X-Gorgon 签名。</summary>
    public static string SignApp(string query, ReadOnlySpan<byte> body, uint timestamp)
    {
        Span<byte> md5Hash = stackalloc byte[16];
        HashQuery(query, md5Hash);
        Span<byte> destination = stackalloc byte[20];
        md5Hash[..4].CopyTo(destination);
        if (!body.IsEmpty)
        {
            Span<byte> bodyHash = stackalloc byte[16];
            MD5.HashData(body, bodyHash);
            bodyHash[..4].CopyTo(destination[4..]);
        }
        destination[12] = 0;
        destination[13] = 6;
        destination[14] = 11;
        destination[15] = 28;
        BinaryPrimitives.WriteUInt32BigEndian(destination[16..], timestamp);
        for (int i = 0; i < destination.Length; i++)
        {
            destination[i] ^= RequestKey[i];
        }
        for (int j = 0; j < destination.Length; j++)
        {
            int num = RotateLeft(destination[j], 4) ^ destination[(j + 1) % destination.Length];
            destination[j] = (byte)(ReverseBits((byte)num) ^ 0xFF ^ destination.Length);
        }
        Span<byte> array3 = stackalloc byte[26];
        array3.Clear();
        array3[0] = 132;
        array3[1] = 4;
        array3[2] = 64;
        array3[3] = 28;
        destination.CopyTo(array3[6..]);
        return Convert.ToHexStringLower(array3);
    }

    /// <summary>为 Passport 请求生成 X-Gorgon 签名。</summary>
    public static string SignPassport(
        string query,
        ReadOnlySpan<byte> body,
        uint timestamp,
        ushort seed
    )
    {
        Span<byte> payload = stackalloc byte[20];
        Span<byte> queryHash = stackalloc byte[16];
        HashQuery(query, queryHash);
        Span<byte> bodyHash = stackalloc byte[16];
        MD5.HashData(body, bodyHash);
        queryHash[..4].CopyTo(payload[..4]);
        bodyHash[..4].CopyTo(payload[4..8]);
        payload[8..12].Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(payload[12..16], 0x04060400);
        BinaryPrimitives.WriteUInt32BigEndian(payload[16..], timestamp);

        Span<byte> key = [0x4A, 0x40, 0x16, (byte)(seed >> 8), 0x47, 0x6C, 0x01, (byte)seed];
        Rc4(payload, key);

        for (int index = 0; index < payload.Length; index++)
        {
            byte next = payload[(index + 1) % payload.Length];
            payload[index] = (byte)(
                ReverseBits((byte)(RotateLeft(payload[index], 4) ^ next)) ^ 0xEB
            );
        }

        Span<byte> signature = stackalloc byte[26];
        signature[0] = 0x84;
        signature[1] = 0x04;
        BinaryPrimitives.WriteUInt16LittleEndian(signature[2..], seed);
        signature[4] = 0x44;
        signature[5] = 0x80;
        payload.CopyTo(signature[6..]);
        return Convert.ToHexStringLower(signature);
    }

    /// <summary>为播放或 video_tab 请求生成 X-Gorgon 签名。</summary>
    public static string SignPlayback(
        string query,
        ReadOnlySpan<byte> body,
        uint timestamp,
        uint random
    )
    {
        Span<byte> queryMd5 = stackalloc byte[16];
        HashQuery(query, queryMd5);

        Span<byte> bodyMd5 = stackalloc byte[16];
        bodyMd5.Clear();
        if (!body.IsEmpty)
        {
            MD5.HashData(body, bodyMd5);
        }

        Span<byte> input = stackalloc byte[20];
        queryMd5[..4].CopyTo(input[..4]);
        bodyMd5[..4].CopyTo(input[4..8]);
        input[8..12].Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(input[12..16], 67503104);
        BinaryPrimitives.WriteUInt32BigEndian(input[16..20], timestamp);

        Span<byte> key =
        [
            0x4A,
            0x40,
            0x16,
            (byte)((random >> 8) & 0xFF),
            0x47,
            0x6C,
            0x01,
            (byte)(random & 0xFF),
        ];
        Rc4(input, key);

        Span<byte> output = stackalloc byte[20];
        input.CopyTo(output);

        for (int i = 0; i < output.Length; i++)
        {
            byte val = output[i];
            byte swapped = (byte)((val >> 4) | (val << 4));
            byte next = (i + 1 < output.Length) ? output[i + 1] : output[0];
            output[i] = (byte)(~(ReverseBits((byte)(next ^ swapped)) ^ 20));
        }

        Span<byte> signature = stackalloc byte[26];
        signature[0] = 0x84;
        signature[1] = 0x04;
        signature[2] = (byte)(random & 0xFF);
        signature[3] = (byte)((random >> 8) & 0xFF);
        signature[4] = 0x40;
        signature[5] = 0x01;
        output.CopyTo(signature[6..]);
        return Convert.ToHexStringLower(signature);
    }

    private static void HashQuery(string query, Span<byte> destination)
    {
        if (query.Length == 0)
        {
            MD5.HashData([], destination);
            return;
        }

        int maxByteCount = Encoding.UTF8.GetMaxByteCount(query.Length);
        if (maxByteCount <= 512)
        {
            Span<byte> utf8Bytes = stackalloc byte[maxByteCount];
            int written = Encoding.UTF8.GetBytes(query, utf8Bytes);
            MD5.HashData(utf8Bytes[..written], destination);
        }
        else
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
            try
            {
                int written = Encoding.UTF8.GetBytes(query, rented);
                MD5.HashData(rented.AsSpan(0, written), destination);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static void Rc4(Span<byte> data, ReadOnlySpan<byte> key)
    {
        Span<byte> sbox = stackalloc byte[256];
        for (int i = 0; i < 256; i++)
        {
            sbox[i] = (byte)i;
        }

        int j = 0;
        for (int i = 0; i < 256; i++)
        {
            j = (j + sbox[i] + key[i % key.Length]) & 0xFF;
            (sbox[i], sbox[j]) = (sbox[j], sbox[i]);
        }

        int si = 0,
            sj = 0;
        for (int k = 0; k < data.Length; k++)
        {
            si = (si + 1) & 0xFF;
            int x = sbox[si];
            sj = (sj + x) & 0xFF;
            int y = sbox[sj];
            sbox[si] = (byte)y;
            sbox[sj] = (byte)x;
            data[k] ^= sbox[(x + y) & 0xFF];
        }
    }

    private static byte ReverseBits(byte b)
    {
        b = (byte)(((b & 0xF0) >> 4) | ((b & 0x0F) << 4));
        b = (byte)(((b & 0xCC) >> 2) | ((b & 0x33) << 2));
        b = (byte)(((b & 0xAA) >> 1) | ((b & 0x55) << 1));
        return b;
    }

    private static int RotateLeft(byte value, int count) =>
        ((value << count) | (value >> (8 - count))) & 0xFF;
}
