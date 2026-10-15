using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>应用请求签名处理。</summary>
internal static class PhoenixReadRequestSigner
{
    private static readonly byte[] RequestKey =
    [
        68,
        185,
        185,
        217,
        164,
        174,
        249,
        252,
        164,
        147,
        170,
        117,
        124,
        163,
        194,
        196,
        164,
        150,
        147,
        143,
    ];
    private static readonly byte[] CommentArgusKey = Convert.FromHexString(
        "ac1adaae95a7af94a5114ab3b3a97dd80050aa0a39314c40528caec95256c28c"
    );

    internal static void Sign(
        HttpRequestMessage request,
        ReadOnlySpan<byte> body,
        DateTimeOffset now
    )
    {
        uint value = checked((uint)now.ToUnixTimeSeconds());
        string s = request.RequestUri?.Query.TrimStart('?') ?? string.Empty;
        byte[] array = MD5.HashData(Encoding.UTF8.GetBytes(s));
        Span<byte> destination = stackalloc byte[20];
        array.AsSpan(0, 4).CopyTo(destination);
        if (!body.IsEmpty)
        {
            byte[] array2 = MD5.HashData(body);
            array2.AsSpan(0, 4).CopyTo(destination[4..]);
            request.Headers.Remove("X-SS-STUB");
            request.Headers.TryAddWithoutValidation("X-SS-STUB", Convert.ToHexString(array2));
        }
        destination[12] = 0;
        destination[13] = 6;
        destination[14] = 11;
        destination[15] = 28;
        BinaryPrimitives.WriteUInt32BigEndian(destination[16..], value);
        for (int i = 0; i < destination.Length; i++)
        {
            destination[i] ^= RequestKey[i];
        }
        for (int j = 0; j < destination.Length; j++)
        {
            int num = RotateLeft(destination[j], 4) ^ destination[(j + 1) % destination.Length];
            destination[j] = (byte)(ReverseBits((byte)num) ^ 0xFF ^ destination.Length);
        }
        byte[] array3 =
        [
            132,
            4,
            64,
            28,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
        ];
        destination.CopyTo(array3.AsSpan(6));
        SetHeader(request, "X-Khronos", value.ToString(CultureInfo.InvariantCulture));
        SetHeader(request, "X-Gorgon", Convert.ToHexString(array3).ToLowerInvariant());
        SetHeader(
            request,
            "X-SS-Req-Ticket",
            now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
    }

    internal static void SignComment(
        HttpRequestMessage request,
        ReadOnlySpan<byte> nonce,
        DateTimeOffset now
    )
    {
        if (nonce.Length != 11)
        {
            throw new ArgumentException("A comment nonce must contain 11 bytes.", nameof(nonce));
        }
        uint num = checked((uint)now.ToUnixTimeSeconds());
        string text = request.RequestUri?.Query.TrimStart('?') ?? string.Empty;
        byte b = (byte)(nonce[0] % 7 + 1 << 5);
        byte b2 = (byte)(0xE0 | (nonce[1] & 0xF));
        Span<byte> span = stackalloc byte[20];
        MD5.HashData(Encoding.UTF8.GetBytes(text), span[..16]);
        span[12] = 0;
        span[13] = 1;
        span[14] = 7;
        span[15] = 4;
        BinaryPrimitives.WriteUInt32BigEndian(span[16..], num);
        InlineArray8<byte> buffer = default;
        buffer[0] = 74;
        buffer[1] = 0;
        buffer[2] = 22;
        buffer[3] = b2;
        buffer[4] = 71;
        buffer[5] = 108;
        buffer[6] = 0;
        buffer[7] = b;
        Span<byte> span2 = buffer;
        Span<byte> span3 = stackalloc byte[256];
        for (int i = 0; i < span3.Length; i++)
        {
            span3[i] = (byte)i;
        }
        byte b3 = 0;
        for (int j = 0; j < span3.Length; j++)
        {
            b3 += (byte)(span3[j] + span2[j & 7]);
            ref byte reference = ref span3[j];
            ref byte reference2 = ref span3[b3];
            byte b4 = span3[b3];
            byte b5 = span3[j];
            reference = b4;
            reference2 = b5;
        }
        b3 = 0;
        for (int k = 0; k < span.Length; k++)
        {
            byte index = (byte)(k + 1);
            b3 += span3[index];
            ref byte reference = ref span3[index];
            ref byte reference3 = ref span3[b3];
            byte b5 = span3[b3];
            byte b4 = span3[index];
            reference = b5;
            reference3 = b4;
            span[k] ^= span3[(byte)(span3[index] + span3[b3])];
        }
        for (int l = 0; l < span.Length; l++)
        {
            span[l] = RotateLeft(span[l], 4);
            span[l] ^= span[(l + 1) % span.Length];
            span[l] = ReverseBits(span[l]);
            span[l] ^= 235;
        }
        byte[] array =
        [
            132,
            4,
            b,
            b2,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
        ];
        span.CopyTo(array.AsSpan(6));
        SetHeader(request, "X-Gorgon", Convert.ToHexString(array).ToLowerInvariant());
        SetHeader(request, "X-Khronos", num.ToString(CultureInfo.InvariantCulture));
        SetHeader(
            request,
            "X-SS-Req-Ticket",
            now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
        SetHeader(request, "X-SS-STUB", string.Empty);
        SetHeader(request, "X-Argus", CommentArgus(text, num, nonce));
        SetHeader(
            request,
            "X-Ladon",
            CommentLadon(GetQueryValue(request.RequestUri, "aid"), num, nonce.Slice(2, 4))
        );
    }

    internal static byte[] CreateCommentNonce()
    {
        byte[] array = new byte[11];
        RandomNumberGenerator.Fill(array);
        return array;
    }

    private static void SetHeader(HttpRequestMessage request, string name, string value)
    {
        request.Headers.Remove(name);
        request.Headers.TryAddWithoutValidation(name, value);
    }

    private static string GetQueryValue(Uri? uri, string key)
    {
        if (uri is null)
        {
            return string.Empty;
        }
        string[] array = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (string text in array)
        {
            string[] array2 = text.Split('=', 2);
            if (Uri.UnescapeDataString(array2[0]) == key)
            {
                return (array2.Length == 2) ? Uri.UnescapeDataString(array2[1]) : string.Empty;
            }
        }
        return string.Empty;
    }

    private static string CommentLadon(string aid, uint timestamp, ReadOnlySpan<byte> prefix)
    {
        byte[] array = new byte[prefix.Length + Encoding.UTF8.GetByteCount(aid)];
        prefix.CopyTo(array);
        Encoding.UTF8.GetBytes(aid, array.AsSpan(prefix.Length));
        string s = Convert.ToHexString(MD5.HashData(array)).ToLowerInvariant();
        byte[] bytes = Encoding.ASCII.GetBytes(s);
        ulong num = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(8));
        Queue<ulong> queue = new([
            BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(16)),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24)),
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
                timestamp.ToString(CultureInfo.InvariantCulture) + "-1611921764-3019"
            )
        );
        for (int j = 0; j < array3.Length; j += 16)
        {
            num = BinaryPrimitives.ReadUInt64LittleEndian(array3.AsSpan(j));
            value = BinaryPrimitives.ReadUInt64LittleEndian(array3.AsSpan(j + 8));
            ulong[] array4 = array2;
            foreach (ulong num3 in array4)
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

    private static string CommentArgus(string query, uint timestamp, ReadOnlySpan<byte> nonce)
    {
        List<byte> list = new(128);
        Varint(list, 1uL, 1077940818uL);
        Varint(list, 2uL, 2uL);
        Varint(list, 3uL, BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(6, 4)));
        Bytes(list, 4uL, "3019"u8);
        Bytes(list, 6uL, "1611921764"u8);
        Bytes(list, 7uL, "6.8.1.32"u8);
        Bytes(list, 8uL, "v04.07.01-ml-android"u8);
        Varint(list, 9uL, 135135744uL);
        Bytes(list, 10uL, new byte[8]);
        Varint(list, 12uL, (ulong)timestamp * 2uL);
        byte[] array = Sm3(new byte[16]);
        byte[] array2 = Sm3(Encoding.UTF8.GetBytes(query));
        Bytes(list, 13uL, array.AsSpan()[..6]);
        Bytes(list, 14uL, array2.AsSpan()[..6]);
        int num = 2;
        List<byte> list2 = new(num);
        CollectionsMarshal.SetCount(list2, num);
        Span<byte> span = CollectionsMarshal.AsSpan(list2);
        span[0] = 8;
        span[1] = (byte)(2 + nonce[10] % 49 * 2);
        List<byte> list3 = list2;
        ulong[] array3 = [2uL, 3uL, 4uL];
        foreach (ulong num2 in array3)
        {
            WriteVarint(list3, num2 * 8);
            WriteVarint(list3, 1388734uL);
        }
        Bytes(list, 15uL, list3.ToArray());
        Bytes(list, 20uL, "none"u8);
        Varint(list, 21uL, 738uL);
        byte[] array4 = Pad([.. list]);
        byte[] array5 = new byte[CommentArgusKey.Length * 2 + 4];
        CommentArgusKey.CopyTo(array5, 0);
        array5[32] = 60;
        array5[33] = 204;
        array5[34] = 86;
        array5[35] = 123;
        CommentArgusKey.CopyTo(array5, 36);
        byte[] array6 = Sm3(array5);
        ulong[] array7 = new ulong[72];
        for (int j = 0; j < 4; j++)
        {
            array7[j] = BinaryPrimitives.ReadUInt64LittleEndian(array6.AsSpan(j * 8));
        }
        for (int k = 4; k < array7.Length; k++)
        {
            ulong num3 = BitOperations.RotateRight(array7[k - 1], 3) ^ array7[k - 3];
            num3 ^= BitOperations.RotateRight(num3, 1);
            array7[k] =
                ~array7[k - 4] ^ num3 ^ (ulong)((4452173518698538891L >>> (k - 4) % 62) & 1) ^ 3;
        }
        for (int l = 0; l < array4.Length; l += 16)
        {
            ulong num4 = BinaryPrimitives.ReadUInt64LittleEndian(array4.AsSpan(l));
            ulong num5 = BinaryPrimitives.ReadUInt64LittleEndian(array4.AsSpan(l + 8));
            ulong[] array8 = array7;
            foreach (ulong num6 in array8)
            {
                ulong num7 = num5;
                ulong num8 =
                    num4
                    ^ (BitOperations.RotateLeft(num5, 1) & BitOperations.RotateLeft(num5, 8))
                    ^ BitOperations.RotateLeft(num5, 2)
                    ^ num6;
                num4 = num7;
                num5 = num8;
            }
            BinaryPrimitives.WriteUInt64LittleEndian(array4.AsSpan(l), num4);
            BinaryPrimitives.WriteUInt64LittleEndian(array4.AsSpan(l + 8), num5);
        }
        ReadOnlySpan<byte> readOnlySpan = [208, 79, 253, 255, 208, 79, 253, 255];
        for (int n = 0; n < array4.Length; n++)
        {
            array4[n] ^= readOnlySpan[n % readOnlySpan.Length];
        }
        byte[] array9 = new byte[readOnlySpan.Length + array4.Length];
        readOnlySpan.CopyTo(array9);
        array4.CopyTo(array9.AsSpan(readOnlySpan.Length));
        Array.Reverse(array9);
        byte[] array10 = new byte[9 + array9.Length + 2];
        new byte[9] { 166, 231, 131, 238, 112, 1, 16, 9, 24 }.CopyTo(array10, 0);
        array9.CopyTo(array10, 9);
        array10[^2] = 86;
        array10[^1] = 123;
        array10 = Pad(array10);
        byte[] key = MD5.HashData(CommentArgusKey[..16]);
        byte[] iV = MD5.HashData(CommentArgusKey[16..]);
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        byte[] array11 = aes.CreateEncryptor().TransformFinalBlock(array10, 0, array10.Length);
        byte[] array12 = new byte[2 + array11.Length];
        array12[0] = 60;
        array12[1] = 204;
        array11.CopyTo(array12, 2);
        return Convert.ToBase64String(array12);
        static void Bytes(List<byte> target, ulong field, ReadOnlySpan<byte> value)
        {
            WriteVarint(target, field * 8 + 2);
            WriteVarint(target, (ulong)value.Length);
            target.AddRange(value.ToArray());
        }
        static void Varint(List<byte> target, ulong field, ulong value)
        {
            WriteVarint(target, field * 8);
            WriteVarint(target, value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte RotateLeft(byte value, int offset)
    {
        return (byte)((value << offset) | (value >> 8 - offset));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ReverseBits(byte value)
    {
        value = (byte)(((value & 0x55) << 1) | ((value & 0xAA) >> 1));
        value = (byte)(((value & 0x33) << 2) | ((value & 0xCC) >> 2));
        return (byte)((value << 4) | (value >> 4));
    }

    private static void WriteVarint(List<byte> target, ulong value)
    {
        while (value >= 128)
        {
            target.Add((byte)(value | 0x80));
            value >>= 7;
        }
        target.Add((byte)value);
    }

    private static byte[] Sm3(ReadOnlySpan<byte> input)
    {
        ulong value = (ulong)input.Length * 8uL;
        int i;
        for (i = input.Length + 1; i % 64 != 56; i++) { }
        byte[] array = new byte[i + 8];
        input.CopyTo(array);
        array[input.Length] = 128;
        BinaryPrimitives.WriteUInt64BigEndian(array.AsSpan(i), value);
        uint[] array2 =
        [
            1937774191u,
            1226093241u,
            388252375u,
            3666478592u,
            2842636476u,
            372324522u,
            3817729613u,
            2969243214u,
        ];
        uint[] array3 = new uint[68];
        for (int j = 0; j < array.Length; j += 64)
        {
            Array.Clear(array3);
            for (int k = 0; k < 16; k++)
            {
                array3[k] = BinaryPrimitives.ReadUInt32BigEndian(array.AsSpan(j + k * 4));
            }
            for (int l = 16; l < array3.Length; l++)
            {
                uint num =
                    array3[l - 16] ^ array3[l - 9] ^ BitOperations.RotateLeft(array3[l - 3], 15);
                array3[l] =
                    num
                    ^ BitOperations.RotateLeft(num, 15)
                    ^ BitOperations.RotateLeft(num, 23)
                    ^ BitOperations.RotateLeft(array3[l - 13], 7)
                    ^ array3[l - 6];
            }
            uint num2 = array2[0];
            uint num3 = array2[1];
            uint num4 = array2[2];
            uint num5 = array2[3];
            uint num6 = array2[4];
            uint num7 = array2[5];
            uint num8 = array2[6];
            uint num9 = array2[7];
            for (int m = 0; m < 64; m++)
            {
                uint num10 = num2;
                uint num11 = num3;
                uint num12 = num4;
                uint num13 = num5;
                uint num14 = num6;
                uint num15 = num7;
                uint num16 = num8;
                uint num17 = num9;
                uint value2 = ((m < 16) ? 2043430169u : 2055708042u);
                uint num18 = (
                    (m < 16)
                        ? (num10 ^ num11 ^ num12)
                        : ((num10 & num11) | (num10 & num12) | (num11 & num12))
                );
                uint num19 = (
                    (m < 16) ? (num14 ^ num15 ^ num16) : ((num14 & num15) | (~num14 & num16))
                );
                uint num20 = BitOperations.RotateLeft(
                    BitOperations.RotateLeft(num10, 12)
                        + num14
                        + BitOperations.RotateLeft(value2, m),
                    7
                );
                uint num21 =
                    num18
                    + num13
                    + (num20 ^ BitOperations.RotateLeft(num10, 12))
                    + (array3[m] ^ array3[m + 4]);
                uint num22 = num19 + num17 + num20 + array3[m];
                num2 = num21;
                num3 = num10;
                num4 = BitOperations.RotateLeft(num11, 9);
                num5 = num12;
                num6 =
                    num22
                    ^ BitOperations.RotateLeft(num22, 9)
                    ^ BitOperations.RotateLeft(num22, 17);
                num7 = num14;
                num8 = BitOperations.RotateLeft(num15, 19);
                num9 = num16;
            }
            array2[0] ^= num2;
            array2[1] ^= num3;
            array2[2] ^= num4;
            array2[3] ^= num5;
            array2[4] ^= num6;
            array2[5] ^= num7;
            array2[6] ^= num8;
            array2[7] ^= num9;
        }
        byte[] array4 = new byte[32];
        for (int n = 0; n < array2.Length; n++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(array4.AsSpan(n * 4), array2[n]);
        }
        return array4;
    }

    private static byte[] Pad(byte[] data)
    {
        int num = 16 - data.Length % 16;
        byte[] array = new byte[data.Length + num];
        data.CopyTo(array, 0);
        Array.Fill(array, (byte)num, data.Length, num);
        return array;
    }
}
