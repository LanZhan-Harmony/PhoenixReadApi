using System.Buffers.Binary;
using System.Buffers.Text;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>
/// 六神算法之 X-Helios 签名器：基于 ARX 置换与密钥扩展的流混淆算法。
/// </summary>
internal static class XHeliosSigner
{
    private const string Aid = "8662";

    /// <summary>为 V5 播放请求生成 X-Helios 签名头。</summary>
    public static string SignV5(uint timestamp, byte[]? randomBytes = null)
    {
        Span<byte> rnd = stackalloc byte[4];
        if (randomBytes is not null && randomBytes.Length >= 4)
        {
            randomBytes.AsSpan(0, 4).CopyTo(rnd);
        }
        else
        {
            RandomNumberGenerator.Fill(rnd);
        }

        Span<byte> seedData = stackalloc byte[8];
        rnd.CopyTo(seedData);
        "8662"u8.CopyTo(seedData[4..]);

        Span<byte> md5 = stackalloc byte[16];
        MD5.HashData(seedData, md5);

        Span<byte> asciiBytes = stackalloc byte[32];
        for (int i = 0; i < 16; i++)
        {
            byte b = md5[i];
            int hi = b >> 4;
            int lo = b & 0xF;
            asciiBytes[i * 2] = (byte)(hi < 10 ? '0' + hi : 'a' + hi - 10);
            asciiBytes[i * 2 + 1] = (byte)(lo < 10 ? '0' + lo : 'a' + lo - 10);
        }

        ulong w0 = BinaryPrimitives.ReadUInt64LittleEndian(asciiBytes[..8]);
        ulong b8 = BinaryPrimitives.ReadUInt64LittleEndian(asciiBytes.Slice(8, 8));
        ulong w2 = BinaryPrimitives.ReadUInt64LittleEndian(asciiBytes.Slice(16, 8));
        ulong w3 = BinaryPrimitives.ReadUInt64LittleEndian(asciiBytes.Slice(24, 8));

        Span<ulong> table = stackalloc ulong[35];
        table[0] = w0;
        ulong b0 = w0;

        for (int i = 0; i < 34; i++)
        {
            ulong x8 = (BitOperations.RotateRight(b8, 8) + b0) ^ (ulong)i;
            ulong x8Table = x8 ^ BitOperations.RotateRight(b0, 61);
            table[i + 1] = x8Table;
            b0 = x8Table;
            b8 = w2;
            w2 = w3;
            w3 = x8;
        }

        Span<byte> textBytes = stackalloc byte[32];
        int textLen = 0;
        if (Utf8Formatter.TryFormat(timestamp, textBytes, out int tsWritten))
        {
            textLen = tsWritten;
        }
        "-646637794-8662"u8.CopyTo(textBytes[textLen..]);
        textLen += 15;

        int padding = 16 - (textLen % 16);
        int paddedLen = textLen + padding;
        Span<byte> padded = stackalloc byte[48];
        textBytes[..textLen].CopyTo(padded);
        padded.Slice(textLen, padding).Fill((byte)padding);

        Span<byte> output = stackalloc byte[4 + 48];
        rnd.CopyTo(output);
        int outPos = 4;

        for (int at = 0; at < paddedLen; at += 16)
        {
            ulong a = BinaryPrimitives.ReadUInt64LittleEndian(padded.Slice(at, 8));
            ulong b = BinaryPrimitives.ReadUInt64LittleEndian(padded.Slice(at + 8, 8));
            for (int i = 0; i < 34; i++)
            {
                b = table[i] ^ (a + BitOperations.RotateRight(b, 8));
                a = b ^ BitOperations.RotateRight(a, 61);
            }
            BinaryPrimitives.WriteUInt64LittleEndian(output.Slice(outPos, 8), a);
            BinaryPrimitives.WriteUInt64LittleEndian(output.Slice(outPos + 8, 8), b);
            outPos += 16;
        }

        return Convert.ToBase64String(output[..outPos]);
    }

    /// <summary>为常规/老版播放或 video_tab 请求生成 X-Helios 签名头。</summary>
    public static string SignPlayback(uint khronos, uint random, string aid = Aid)
    {
        Span<byte> seed = stackalloc byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(seed[..4], random);
        Encoding.ASCII.GetBytes(aid).CopyTo(seed[4..]);

        Span<byte> digest = stackalloc byte[16];
        MD5.HashData(seed, digest);

        Span<byte> ascii = stackalloc byte[32];
        Encoding.ASCII.GetBytes(Convert.ToHexStringLower(digest), ascii);

        Span<ulong> words = stackalloc ulong[4];
        for (int i = 0; i < 4; i++)
        {
            words[i] = BinaryPrimitives.ReadUInt64LittleEndian(ascii[(i * 8)..]);
        }

        Span<ulong> table = stackalloc ulong[35];
        table[0] = words[0];
        ulong b0 = words[0];
        ulong b8 = words[1];
        Span<ulong> queue = stackalloc ulong[36];
        queue[0] = words[2];
        queue[1] = words[3];
        int queueHead = 0;
        int queueTail = 2;

        for (ulong i = 0; i < 34; i++)
        {
            ulong x8 = BitOperations.RotateRight(b8, 8) + b0;
            x8 ^= i;
            queue[queueTail++] = x8;
            x8 ^= BitOperations.RotateRight(b0, 61);
            table[(int)(i + 1)] = x8;
            b0 = x8;
            b8 = queue[queueHead++];
        }

        string text = $"{khronos}-1588093228-{aid}";
        byte fill = (byte)(16 - (text.Length % 16));
        int size = ((text.Length + 1 + 15) / 16) * 16;
        Span<byte> padded = stackalloc byte[size];
        padded.Fill(fill);
        Encoding.UTF8.GetBytes(text, padded);

        byte[] result = new byte[4 + size];
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(0, 4), random);

        for (int off = 0; off < size; off += 16)
        {
            ulong a = BinaryPrimitives.ReadUInt64LittleEndian(padded[off..]);
            ulong b = BinaryPrimitives.ReadUInt64LittleEndian(padded[(off + 8)..]);
            for (int t = 0; t < 34; t++)
            {
                b = table[t] ^ (a + BitOperations.RotateRight(b, 8));
                a = b ^ BitOperations.RotateRight(a, 61);
            }
            BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(4 + off, 8), a);
            BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(4 + off + 8, 8), b);
        }

        return Convert.ToBase64String(result);
    }
}
