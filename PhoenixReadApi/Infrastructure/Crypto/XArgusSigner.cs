using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>
/// 六神算法之 X-Argus 签名器：基于 Simon 分组密码、SM3 摘要与 AES-CBC/CTR 混合加密。
/// </summary>
internal static class XArgusSigner
{
    private const string PlaybackLicenseId = "1611921764";

    private static readonly byte[] CommentArgusKey = Convert.FromHexString(
        "ac1adaae95a7af94a5114ab3b3a97dd80050aa0a39314c40528caec95256c28c"
    );
    private static readonly byte[] CommentArgusAesKey = MD5.HashData(CommentArgusKey[..16]);
    private static readonly byte[] CommentArgusAesIv = MD5.HashData(CommentArgusKey[16..]);
    private static readonly byte[] StaticDerivedArgusKey = ComputeDerivedArgusKey();
    private static readonly ulong[] PlayerArgusSimonRoundKeys = PrecomputePlayerSimonRoundKeys();
    private static readonly byte[] StaticCounters = ComputeStaticCounters();
    private static readonly byte[] StaticEnvironment = ComputeStaticEnvironment();
    private static readonly byte[] StaticZero16Sm3 = Sm3(new byte[16]);

    private static readonly byte[] PlaybackArgusSignKey = Convert.FromHexString(
        "fc78e0a9657a0c748ce51559903ccf03510e51d3cff232d71343e88a321c5304"
    );
    private static readonly byte[] PlaybackArgusAesKey = Convert.FromHexString(
        "d31e3718288a1027baab59f146a09a9c"
    );
    private static readonly byte[] PlaybackArgusAesIv = Convert.FromHexString(
        "ea180a0336ed352fcd24e4d50018ae54"
    );
    private static readonly byte[] PlaybackArgusMask =
    [
        0xf2,
        0xf7,
        0xfc,
        0xff,
        0xf2,
        0xf7,
        0xfc,
        0xff,
    ];
    private static readonly byte[] PlaybackArgusTail =
    [
        0xea,
        0xfb,
        0x2c,
        0xfe,
        0x85,
        0x68,
        0x51,
        0x91,
        0x54,
    ];
    private static readonly ulong[] PlaybackArgusSimonRoundKeys =
        PrecomputePlaybackSimonRoundKeys();
    private static readonly byte[] PlaybackCounters = PrecomputePlaybackCounters();
    private static readonly byte[] PlaybackDevice = PrecomputePlaybackDevice();

    /// <summary>为播放或 video_tab 请求生成 X-Argus 签名。</summary>
    public static string SignPlayback(
        string query,
        ReadOnlySpan<byte> bodyMd5,
        uint timestamp,
        uint nonce,
        string aid,
        string deviceId,
        string installId
    )
    {
        Span<byte> bodyHash = stackalloc byte[32];
        if (bodyMd5.IsEmpty)
        {
            StaticZero16Sm3.CopyTo(bodyHash);
        }
        else
        {
            Sm3(bodyMd5, bodyHash);
        }

        Span<byte> queryHash = stackalloc byte[32];
        if (string.IsNullOrEmpty(query))
        {
            Span<byte> zeros16 = stackalloc byte[16];
            zeros16.Clear();
            Sm3(zeros16, queryHash);
        }
        else
        {
            int maxQueryBytes = Encoding.UTF8.GetMaxByteCount(query.Length);
            if (maxQueryBytes <= 512)
            {
                Span<byte> queryBytes = stackalloc byte[maxQueryBytes];
                int written = Encoding.UTF8.GetBytes(query, queryBytes);
                Sm3(queryBytes[..written], queryHash);
            }
            else
            {
                byte[] rented = ArrayPool<byte>.Shared.Rent(maxQueryBytes);
                try
                {
                    int written = Encoding.UTF8.GetBytes(query, rented);
                    Sm3(rented.AsSpan(0, written), queryHash);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        string versionName = GetQueryValue(query, "version_name");

        List<byte> bean = [with(384)];
        Varint(bean, 1, 1077940818UL);
        Varint(bean, 2, 2UL);
        Varint(bean, 3, nonce);
        PbString(bean, 4, aid);
        PbString(bean, 5, deviceId);
        PbString(bean, 6, PlaybackLicenseId);
        PbString(bean, 7, versionName);
        Bytes(bean, 8, "v04.04.05-ov-android"u8);
        Varint(bean, 9, 134744640UL);
        Bytes(bean, 10, stackalloc byte[8]);
        Varint(bean, 11, 0UL);
        Varint(bean, 12, timestamp << 1);
        Bytes(bean, 13, bodyHash[..6]);
        Bytes(bean, 14, queryHash[..6]);
        Bytes(bean, 15, PlaybackCounters);
        PbString(bean, 16, installId);
        Bytes(bean, 20, "none"u8);
        Varint(bean, 21, 738UL);
        Bytes(bean, 23, PlaybackDevice);
        Varint(bean, 25, 2UL);

        byte[] plain = Pad(CollectionsMarshal.AsSpan(bean));
        SimonEncrypt(plain, PlaybackArgusSimonRoundKeys);

        byte[] frame = new byte[PlaybackArgusMask.Length + plain.Length + PlaybackArgusTail.Length];
        PlaybackArgusMask.CopyTo(frame, 0);
        plain.CopyTo(frame, PlaybackArgusMask.Length);
        PlaybackArgusTail.CopyTo(frame, PlaybackArgusMask.Length + plain.Length);

        for (int i = PlaybackArgusMask.Length; i < frame.Length; i++)
        {
            frame[i] ^= PlaybackArgusMask[i % PlaybackArgusMask.Length];
        }

        Array.Reverse(frame);

        byte[] frameWithMarker = new byte[frame.Length + 2];
        frame.CopyTo(frameWithMarker, 0);
        frameWithMarker[^2] = 0x61;
        frameWithMarker[^1] = 0x6f;

        byte[] paddedFrame = Pad(frameWithMarker);

        using Aes aes = Aes.Create();
        aes.Key = PlaybackArgusAesKey;
        aes.IV = PlaybackArgusAesIv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        byte[] encrypted = aes.CreateEncryptor()
            .TransformFinalBlock(paddedFrame, 0, paddedFrame.Length);

        byte[] result = new byte[2 + encrypted.Length];
        result[0] = 0xf2;
        result[1] = 0x81;
        encrypted.CopyTo(result, 2);

        return Convert.ToBase64String(result);
    }

    /// <summary>为 Player 协议请求生成 X-Argus 签名。</summary>
    public static string SignPlayer(
        string query,
        ReadOnlySpan<byte> body,
        uint timestamp,
        string deviceId,
        ReadOnlySpan<byte> nonce
    )
    {
        Span<byte> bodyHash = stackalloc byte[32];
        if (body.IsEmpty)
        {
            StaticZero16Sm3.CopyTo(bodyHash);
        }
        else
        {
            Span<byte> bodyMd5 = stackalloc byte[16];
            MD5.HashData(body, bodyMd5);
            Sm3(bodyMd5, bodyHash);
        }

        Span<byte> queryHash = stackalloc byte[32];
        int maxQueryBytes = Encoding.UTF8.GetMaxByteCount(query.Length);
        if (maxQueryBytes <= 512)
        {
            Span<byte> queryBytes = stackalloc byte[maxQueryBytes];
            int written = Encoding.UTF8.GetBytes(query, queryBytes);
            Sm3(queryBytes[..written], queryHash);
        }
        else
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(maxQueryBytes);
            try
            {
                int written = Encoding.UTF8.GetBytes(query, rented);
                Sm3(rented.AsSpan(0, written), queryHash);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        List<byte> message = [with(256)];
        Varint(message, 1uL, 0x20200929uL * 2uL);
        Varint(message, 2uL, 2uL);
        Varint(
            message,
            3uL,
            BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(6, 4)) & 0x7fffffffu
        );
        Bytes(message, 4uL, "8662"u8);
        Span<byte> devBytes = stackalloc byte[Encoding.UTF8.GetMaxByteCount(deviceId.Length)];
        int devLen = Encoding.UTF8.GetBytes(deviceId, devBytes);
        Bytes(message, 5uL, devBytes[..devLen]);
        Bytes(message, 6uL, "1611921764"u8);
        Bytes(message, 7uL, "7.3.9.32"u8);
        Bytes(message, 8uL, "v04.07.01-ml-android"u8);
        Varint(message, 9uL, 135135744uL);
        Bytes(message, 10uL, stackalloc byte[8]);
        Varint(message, 11uL, 0uL);
        Varint(message, 12uL, timestamp * 2uL);
        Bytes(message, 13uL, bodyHash[..6]);
        Bytes(message, 14uL, queryHash[..6]);
        Bytes(message, 15uL, StaticCounters);
        Bytes(message, 16uL, []);
        Bytes(message, 20uL, "none"u8);
        Varint(message, 21uL, 738uL);
        Bytes(message, 23uL, StaticEnvironment);
        Varint(message, 25uL, 2uL);

        byte[] data = Pad(CollectionsMarshal.AsSpan(message));
        ReadOnlySpan<ulong> rounds = PlayerArgusSimonRoundKeys;

        for (int l = 0; l < data.Length; l += 16)
        {
            ulong left = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(l));
            ulong right = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(l + 8));
            foreach (ulong round in rounds)
            {
                right = round ^ (left + BitOperations.RotateRight(right, 8));
                left = right ^ BitOperations.RotateLeft(left, 3);
            }
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(l), left);
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(l + 8), right);
        }

        byte[] destination = new byte[8 + data.Length + 10];
        CommentArgusKey.AsSpan(0, 8).CopyTo(destination);
        data.CopyTo(destination.AsSpan(8));
        CommentArgusKey.AsSpan(16, 10).CopyTo(destination.AsSpan(8 + data.Length));

        for (int m = 8; m < destination.Length; m++)
        {
            destination[m] ^= destination[m % 8];
        }

        Array.Reverse(destination);
        byte[] array4 = new byte[destination.Length + 2];
        destination.CopyTo(array4, 0);
        array4[^2] = 0x61;
        array4[^1] = 0x6f;

        byte[] block = Pad(array4);
        using Aes aes = Aes.Create();
        aes.Key = CommentArgusAesKey;
        aes.IV = CommentArgusAesIv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        byte[] cipher = aes.CreateEncryptor().TransformFinalBlock(block, 0, block.Length);

        byte[] result = new byte[2 + cipher.Length];
        result[0] = 0xf2;
        result[1] = 0x81;
        cipher.CopyTo(result.AsSpan(2));
        return Convert.ToBase64String(result);
    }

    private static void SimonEncrypt(Span<byte> plain, ReadOnlySpan<ulong> roundKeys)
    {
        for (int at = 0; at < plain.Length; at += 16)
        {
            ulong a = BinaryPrimitives.ReadUInt64LittleEndian(plain[at..]);
            ulong b = BinaryPrimitives.ReadUInt64LittleEndian(plain[(at + 8)..]);
            for (int i = 0; i < 68; i++)
            {
                ulong tmp = BitOperations.RotateLeft(a, 1) & BitOperations.RotateLeft(a, 8);
                tmp ^= BitOperations.RotateLeft(a, 2);
                tmp ^= b;
                tmp ^= roundKeys[i];
                b = a;
                a = tmp;
            }
            BinaryPrimitives.WriteUInt64LittleEndian(plain[at..], a);
            BinaryPrimitives.WriteUInt64LittleEndian(plain[(at + 8)..], b);
        }
    }

    private static byte[] ComputeDerivedArgusKey()
    {
        byte[] seed = new byte[CommentArgusKey.Length * 2 + 4];
        CommentArgusKey.CopyTo(seed, 0);
        seed[32] = 0x3C;
        seed[33] = 0xCC;
        seed[34] = 0x56;
        seed[35] = 0x7B;
        CommentArgusKey.CopyTo(seed, 36);
        return Sm3(seed);
    }

    private static ulong[] PrecomputePlayerSimonRoundKeys()
    {
        ulong[] rounds = new ulong[72];
        for (int i = 0; i < 4; i++)
        {
            rounds[i] = BinaryPrimitives.ReadUInt64LittleEndian(
                StaticDerivedArgusKey.AsSpan(i * 8, 8)
            );
        }
        for (int k = 4; k < 72; k++)
        {
            ulong mixed = BitOperations.RotateRight(rounds[k - 1], 3) ^ rounds[k - 3];
            mixed ^= BitOperations.RotateRight(mixed, 1);
            rounds[k] = ~rounds[k - 4] ^ mixed ^ (0x3dc94c3a046d678bUL >>> ((k - 4) % 62)) & 1 ^ 3;
        }
        return rounds;
    }

    private static ulong[] PrecomputePlaybackSimonRoundKeys()
    {
        ulong[] k = new ulong[72];
        for (int i = 0; i < 4; i++)
        {
            k[i] = BinaryPrimitives.ReadUInt64LittleEndian(PlaybackArgusSignKey.AsSpan(i * 8, 8));
        }

        const ulong z = 0x3dc94c3a046d678bUL;
        for (int i = 4; i < 72; i++)
        {
            ulong x = BitOperations.RotateRight(k[i - 1], 3) ^ k[i - 3];
            x ^= BitOperations.RotateRight(x, 1);
            k[i] = ~k[i - 4] ^ x ^ ((z >> ((i - 4) % 62)) & 1UL) ^ 3UL;
        }

        return k;
    }

    private static byte[] PrecomputePlaybackCounters()
    {
        List<byte> list = [with(32)];
        Varint(list, 1, 1);
        Varint(list, 2, 1);
        Varint(list, 3, 1);
        Varint(list, 7, 0xc792ecccUL);
        return [.. list];
    }

    private static byte[] PrecomputePlaybackDevice()
    {
        List<byte> list = [with(32)];
        Bytes(list, 1, "NX551J"u8);
        Varint(list, 2, 8196);
        Varint(list, 4, 0x80e0d800UL);
        return [.. list];
    }

    private static byte[] ComputeStaticCounters()
    {
        List<byte> list = [with(16)];
        Varint(list, 1uL, 1uL);
        Varint(list, 2uL, 1uL);
        Varint(list, 3uL, 1uL);
        Varint(list, 7uL, 3348327628uL);
        return [.. list];
    }

    private static byte[] ComputeStaticEnvironment()
    {
        List<byte> list = [with(16)];
        Bytes(list, 1uL, "NX551J"u8);
        Varint(list, 2uL, 8196uL);
        Varint(list, 4uL, 2162227200uL);
        return [.. list];
    }

    private static void Varint(List<byte> buffer, ulong fieldNumber, ulong value)
    {
        ulong tag = (fieldNumber << 3) | 0;
        WriteVarint(buffer, tag);
        WriteVarint(buffer, value);
    }

    private static void Bytes(List<byte> buffer, ulong fieldNumber, ReadOnlySpan<byte> value)
    {
        ulong tag = (fieldNumber << 3) | 2;
        WriteVarint(buffer, tag);
        WriteVarint(buffer, (ulong)value.Length);
        buffer.AddRange(value);
    }

    private static void PbString(List<byte> buffer, ulong fieldNumber, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }
        int maxBytes = Encoding.UTF8.GetMaxByteCount(value.Length);
        Span<byte> span = stackalloc byte[maxBytes];
        int written = Encoding.UTF8.GetBytes(value, span);
        Bytes(buffer, fieldNumber, span[..written]);
    }

    private static void WriteVarint(List<byte> buffer, ulong value)
    {
        while (value >= 0x80)
        {
            buffer.Add((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }
        buffer.Add((byte)value);
    }

    private static byte[] Pad(ReadOnlySpan<byte> data)
    {
        int pad = 16 - (data.Length % 16);
        byte[] result = new byte[data.Length + pad];
        data.CopyTo(result);
        result.AsSpan(data.Length).Fill((byte)pad);
        return result;
    }

    private static byte[] Sm3(ReadOnlySpan<byte> input)
    {
        byte[] result = new byte[32];
        PhoenixReadRequestSigner.Sm3(input, result);
        return result;
    }

    private static void Sm3(ReadOnlySpan<byte> input, Span<byte> destination) =>
        PhoenixReadRequestSigner.Sm3(input, destination);

    private static string GetQueryValue(string query, string key)
    {
        int start = 0;
        while (start < query.Length)
        {
            int eq = query.IndexOf('=', start);
            if (eq < 0)
            {
                break;
            }
            int amp = query.IndexOf('&', eq);
            if (amp < 0)
            {
                amp = query.Length;
            }
            if (string.Equals(query[start..eq], key, StringComparison.Ordinal))
            {
                return Uri.UnescapeDataString(query[(eq + 1)..amp].Replace('+', ' '));
            }
            start = amp + 1;
        }
        return string.Empty;
    }
}
