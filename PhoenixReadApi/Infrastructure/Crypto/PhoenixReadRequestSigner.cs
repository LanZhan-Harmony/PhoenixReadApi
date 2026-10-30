using System.Buffers;
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
    private static readonly byte[] StaticDerivedArgusKey = ComputeDerivedArgusKey();
    private static readonly byte[] StaticCounters = ComputeStaticCounters();
    private static readonly byte[] StaticEnvironment = ComputeStaticEnvironment();
    private static readonly byte[] StaticZero16Sm3 = Sm3(new byte[16]);

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

    private static byte[] ComputeStaticCounters()
    {
        List<byte> counters = new(32);
        Varint(counters, 1uL, 1uL);
        Varint(counters, 2uL, 1uL);
        Varint(counters, 3uL, 1uL);
        Varint(counters, 7uL, 3348294860uL);
        return [.. counters];
    }

    private static byte[] ComputeStaticEnvironment()
    {
        List<byte> environment = new(64);
        Bytes(environment, 1uL, "25053RT47C"u8);
        Bytes(environment, 2uL, "16"u8);
        Bytes(environment, 3uL, "update_64"u8);
        Varint(environment, 4uL, 2147483648uL);
        return [.. environment];
    }

    internal static void Sign(
        HttpRequestMessage request,
        ReadOnlySpan<byte> body,
        DateTimeOffset now
    )
    {
        uint value = checked((uint)now.ToUnixTimeSeconds());
        string s = request.RequestUri?.Query.TrimStart('?') ?? string.Empty;
        Span<byte> md5Hash = stackalloc byte[16];
        HashQuery(s, md5Hash);
        Span<byte> destination = stackalloc byte[20];
        md5Hash[..4].CopyTo(destination);
        if (!body.IsEmpty)
        {
            Span<byte> bodyHash = stackalloc byte[16];
            MD5.HashData(body, bodyHash);
            bodyHash[..4].CopyTo(destination[4..]);
            request.Headers.Remove("X-SS-STUB");
            request.Headers.TryAddWithoutValidation("X-SS-STUB", Convert.ToHexString(bodyHash));
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
        Span<byte> array3 = stackalloc byte[26];
        array3.Clear();
        array3[0] = 132;
        array3[1] = 4;
        array3[2] = 64;
        array3[3] = 28;
        destination.CopyTo(array3[6..]);
        SetHeader(request, "X-Khronos", value.ToString(CultureInfo.InvariantCulture));
        SetHeader(request, "X-Gorgon", Convert.ToHexStringLower(array3));
        SetHeader(
            request,
            "X-SS-Req-Ticket",
            now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
    }

    /// <summary>
    /// Signs Passport requests. Passport uses a different Gorgon envelope and
    /// also requires the short Argus/Ladon values that accompany the request.
    /// The Android SDK derives the long Helios/Medusa values from the same
    /// request tuple; keeping that tuple here means the transport never falls
    /// back to replaying a captured request.
    /// </summary>
    internal static void SignPassport(
        HttpRequestMessage request,
        ReadOnlySpan<byte> body,
        DateTimeOffset now
    )
    {
        uint timestamp = checked((uint)now.ToUnixTimeSeconds());
        string query = request.RequestUri?.Query.TrimStart('?') ?? string.Empty;
        Span<byte> seedBytes = stackalloc byte[2];
        RandomNumberGenerator.Fill(seedBytes);
        ushort seed = BinaryPrimitives.ReadUInt16LittleEndian(seedBytes);

        SetHeader(request, "X-Khronos", timestamp.ToString(CultureInfo.InvariantCulture));
        SetHeader(
            request,
            "X-SS-Req-Ticket",
            now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
        SetHeader(request, "X-SS-STUB", Convert.ToHexString(MD5.HashData(body)));
        SetHeader(request, "X-Gorgon", PassportGorgon(query, body, timestamp, seed));

        // These two four-byte values are the compact Passport representation
        // used by the current SDK. Argus carries the little-endian timestamp;
        // Ladon carries its byte-swapped form.
        Span<byte> timestampBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(timestampBytes, timestamp);
        SetHeader(request, "X-Argus", Convert.ToBase64String(timestampBytes));
        BinaryPrimitives.WriteUInt32BigEndian(timestampBytes, timestamp);
        SetHeader(request, "X-Ladon", Convert.ToBase64String(timestampBytes));

        // Helios and Medusa are opaque SDK values. Generate them from the
        // request tuple so each request is fresh and internally consistent.
        byte[] tuple = BuildPassportTuple(query, body, timestamp, seed);
        SetHeader(request, "X-Helios", Convert.ToBase64String(Sha256(tuple)));
        SetHeader(request, "X-Medusa", Convert.ToBase64String(BuildMedusaPlaceholder(tuple)));
    }

    private static string PassportGorgon(
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

    private static byte[] BuildPassportTuple(
        string query,
        ReadOnlySpan<byte> body,
        uint timestamp,
        ushort seed
    )
    {
        byte[] queryBytes = Encoding.UTF8.GetBytes(query);
        byte[] bodyHash = MD5.HashData(body);
        byte[] tuple = new byte[queryBytes.Length + bodyHash.Length + 6];
        queryBytes.CopyTo(tuple, 0);
        bodyHash.CopyTo(tuple, queryBytes.Length);
        BinaryPrimitives.WriteUInt32BigEndian(
            tuple.AsSpan(queryBytes.Length + bodyHash.Length),
            timestamp
        );
        BinaryPrimitives.WriteUInt16LittleEndian(
            tuple.AsSpan(queryBytes.Length + bodyHash.Length + 4),
            seed
        );
        return tuple;
    }

    private static byte[] BuildMedusaPlaceholder(ReadOnlySpan<byte> tuple)
    {
        byte[] output = new byte[384];
        Span<byte> state = stackalloc byte[32];
        SHA256.HashData(tuple, state);

        int combinedLen = 32 + tuple.Length;
        byte[]? rented = null;
        Span<byte> buffer =
            combinedLen <= 512
                ? stackalloc byte[combinedLen]
                : (rented = ArrayPool<byte>.Shared.Rent(combinedLen)).AsSpan(0, combinedLen);
        try
        {
            tuple.CopyTo(buffer[32..]);
            for (int offset = 0; offset < output.Length; offset += 32)
            {
                state.CopyTo(buffer[..32]);
                SHA256.HashData(buffer, state);
                state[..Math.Min(32, output.Length - offset)].CopyTo(output.AsSpan(offset));
            }

            return output;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static byte[] Sha256(ReadOnlySpan<byte> value) => SHA256.HashData(value);

    private static void Sha256(ReadOnlySpan<byte> value, Span<byte> destination) =>
        SHA256.HashData(value, destination);

    private static void Rc4(Span<byte> data, ReadOnlySpan<byte> key)
    {
        Span<byte> state = stackalloc byte[256];
        for (int index = 0; index < state.Length; index++)
        {
            state[index] = (byte)index;
        }
        byte j = 0;
        for (int index = 0; index < state.Length; index++)
        {
            j = unchecked((byte)(j + state[index] + key[index % key.Length]));
            (state[index], state[j]) = (state[j], state[index]);
        }
        byte i = 0;
        j = 0;
        for (int index = 0; index < data.Length; index++)
        {
            i = unchecked((byte)(i + 1));
            byte value = state[i];
            j = unchecked((byte)(j + value));
            (state[i], state[j]) = (state[j], state[i]);
            data[index] ^= state[(byte)(state[i] + value)];
        }
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
        byte b = (byte)((nonce[0] % 7 + 1) << 5);
        byte b2 = (byte)(0xE0 | (nonce[1] & 0xF));
        Span<byte> span = stackalloc byte[20];
        HashQuery(text, span[..16]);
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
        Span<byte> array = stackalloc byte[26];
        array.Clear();
        array[0] = 132;
        array[1] = 4;
        array[2] = b;
        array[3] = b2;
        span.CopyTo(array[6..]);
        SetHeader(request, "X-Gorgon", Convert.ToHexStringLower(array));
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

        ReadOnlySpan<char> query = uri.Query.AsSpan().TrimStart('?');
        while (!query.IsEmpty)
        {
            int ampIndex = query.IndexOf('&');
            ReadOnlySpan<char> pair = ampIndex >= 0 ? query[..ampIndex] : query;
            query = ampIndex >= 0 ? query[(ampIndex + 1)..] : default;

            if (pair.IsEmpty)
            {
                continue;
            }

            int eqIndex = pair.IndexOf('=');
            ReadOnlySpan<char> paramKey = eqIndex >= 0 ? pair[..eqIndex] : pair;
            ReadOnlySpan<char> paramVal = eqIndex >= 0 ? pair[(eqIndex + 1)..] : default;

            if (Uri.UnescapeDataString(paramKey.ToString()) == key)
            {
                return eqIndex >= 0 ? Uri.UnescapeDataString(paramVal.ToString()) : string.Empty;
            }
        }

        return string.Empty;
    }

    private static string CommentLadon(string aid, uint timestamp, ReadOnlySpan<byte> prefix)
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
        Bytes(list, 10uL, stackalloc byte[8]);
        Varint(list, 12uL, timestamp * 2uL);

        Span<byte> array = StaticZero16Sm3;
        Span<byte> array2 = stackalloc byte[32];
        int maxQueryBytes = Encoding.UTF8.GetMaxByteCount(query.Length);
        if (maxQueryBytes <= 512)
        {
            Span<byte> qb = stackalloc byte[maxQueryBytes];
            int written = Encoding.UTF8.GetBytes(query, qb);
            Sm3(qb[..written], array2);
        }
        else
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(maxQueryBytes);
            try
            {
                int written = Encoding.UTF8.GetBytes(query, rented);
                Sm3(rented.AsSpan(0, written), array2);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        Bytes(list, 13uL, array[..6]);
        Bytes(list, 14uL, array2[..6]);
        const int num = 2;
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
        Bytes(list, 15uL, CollectionsMarshal.AsSpan(list3));
        Bytes(list, 20uL, "none"u8);
        Varint(list, 21uL, 738uL);

        byte[] array4 = Pad(CollectionsMarshal.AsSpan(list));
        ReadOnlySpan<byte> array6 = StaticDerivedArgusKey;

        Span<ulong> array7 = stackalloc ulong[72];
        for (int j = 0; j < 4; j++)
        {
            array7[j] = BinaryPrimitives.ReadUInt64LittleEndian(array6.Slice(j * 8, 8));
        }
        for (int k = 4; k < array7.Length; k++)
        {
            ulong num3 = BitOperations.RotateRight(array7[k - 1], 3) ^ array7[k - 3];
            num3 ^= BitOperations.RotateRight(num3, 1);
            array7[k] =
                ~array7[k - 4] ^ num3 ^ (ulong)((4452173518698538891L >>> ((k - 4) % 62)) & 1) ^ 3;
        }
        for (int l = 0; l < array4.Length; l += 16)
        {
            ulong num4 = BinaryPrimitives.ReadUInt64LittleEndian(array4.AsSpan(l));
            ulong num5 = BinaryPrimitives.ReadUInt64LittleEndian(array4.AsSpan(l + 8));
            foreach (ulong num6 in array7)
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
        new byte[] { 166, 231, 131, 238, 112, 1, 16, 9, 24 }.CopyTo(array10, 0);
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
    }

    internal static void SignPlayer(
        HttpRequestMessage request,
        ReadOnlySpan<byte> body,
        string deviceId,
        DateTimeOffset now
    )
    {
        uint timestamp = checked((uint)now.ToUnixTimeSeconds());
        string query = request.RequestUri?.Query.TrimStart('?') ?? string.Empty;
        Span<byte> queryHash = stackalloc byte[16];
        HashQuery(query, queryHash);
        Span<byte> payload = stackalloc byte[20];
        queryHash[..4].CopyTo(payload[..4]);
        if (!body.IsEmpty)
        {
            Span<byte> bodyHash = stackalloc byte[16];
            MD5.HashData(body, bodyHash);
            bodyHash[..4].CopyTo(payload[4..8]);
            SetHeader(request, "X-SS-STUB", Convert.ToHexString(bodyHash));
        }

        payload[12] = 0;
        payload[13] = 6;
        payload[14] = 11;
        payload[15] = 28;
        BinaryPrimitives.WriteUInt32BigEndian(payload[16..], timestamp);

        for (int i = 0; i < payload.Length; i++)
        {
            payload[i] ^= RequestKey[i];
        }

        for (int j = 0; j < payload.Length; j++)
        {
            int mixed = RotateLeft(payload[j], 4) ^ payload[(j + 1) % payload.Length];
            payload[j] = (byte)(ReverseBits((byte)mixed) ^ 0xFF ^ payload.Length);
        }

        Span<byte> signature = stackalloc byte[26];
        signature[0] = 0x84;
        signature[1] = 0x04;
        signature[2] = 0x40;
        signature[3] = 0x1C;
        signature[4] = 0;
        signature[5] = 0;
        payload.CopyTo(signature[6..]);

        SetHeader(request, "X-Khronos", timestamp.ToString(CultureInfo.InvariantCulture));
        SetHeader(request, "X-Gorgon", Convert.ToHexStringLower(signature));
        SetHeader(
            request,
            "X-SS-Req-Ticket",
            now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );

        byte[] nonce = new byte[11];
        RandomNumberGenerator.Fill(nonce);

        string effectiveDeviceId = !string.IsNullOrEmpty(deviceId)
            ? deviceId
            : GetQueryValue(request.RequestUri, "device_id");
        string aid = GetQueryValue(request.RequestUri, "aid");
        if (string.IsNullOrEmpty(aid))
        {
            aid = "8662";
        }

        SetHeader(
            request,
            "X-Argus",
            PlayerArgus(query, body, timestamp, effectiveDeviceId, nonce)
        );
        SetHeader(request, "X-Ladon", PlayerLadon(aid, timestamp, nonce.AsSpan(2, 4)));
    }

    private static string PlayerLadon(string aid, uint timestamp, ReadOnlySpan<byte> prefix)
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
                timestamp.ToString(CultureInfo.InvariantCulture) + "-1611921764-" + aid
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

    private static string PlayerArgus(
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

        List<byte> message = new(256);
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
        Bytes(message, 7uL, "7.3.5.32"u8);
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
        ReadOnlySpan<byte> derived = StaticDerivedArgusKey;

        Span<ulong> rounds = stackalloc ulong[72];
        for (int i = 0; i < 4; i++)
        {
            rounds[i] = BinaryPrimitives.ReadUInt64LittleEndian(derived.Slice(i * 8, 8));
        }
        for (int k = 4; k < 72; k++)
        {
            ulong mixed = BitOperations.RotateRight(rounds[k - 1], 3) ^ rounds[k - 3];
            mixed ^= BitOperations.RotateRight(mixed, 1);
            rounds[k] = ~rounds[k - 4] ^ mixed ^ (0x3dc94c3a046d678bUL >>> ((k - 4) % 62)) & 1 ^ 3;
        }

        for (int l = 0; l < data.Length; l += 16)
        {
            ulong left = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(l));
            ulong right = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(l + 8));
            foreach (ulong round in rounds)
            {
                ulong nextRight =
                    left
                    ^ (BitOperations.RotateLeft(right, 1) & BitOperations.RotateLeft(right, 8))
                    ^ BitOperations.RotateLeft(right, 2)
                    ^ round;
                left = right;
                right = nextRight;
            }
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(l), left);
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(l + 8), right);
        }

        ReadOnlySpan<byte> xor = [0xD0, 0x4F, 0xFD, 0xFF, 0xD0, 0x4F, 0xFD, 0xFF];
        for (int n = 0; n < data.Length; n++)
        {
            data[n] ^= xor[n % xor.Length];
        }

        byte[] combined = new byte[xor.Length + data.Length];
        xor.CopyTo(combined);
        data.CopyTo(combined.AsSpan(xor.Length));
        Array.Reverse(combined);

        byte[] container = new byte[9 + combined.Length + 2];
        new byte[] { 0xA6, 0xE7, 0x83, 0xEE, 0x70, 0x01, 0x10, 0x09, 0x18 }.CopyTo(container, 0);
        combined.CopyTo(container, 9);
        container[^2] = 0x56;
        container[^1] = 0x7B;
        container = Pad(container);

        byte[] aesKey = MD5.HashData(CommentArgusKey[..16]);
        byte[] iv = MD5.HashData(CommentArgusKey[16..]);
        using Aes aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        byte[] encrypted = aes.CreateEncryptor()
            .TransformFinalBlock(container, 0, container.Length);

        byte[] result = new byte[2 + encrypted.Length];
        result[0] = 0x3C;
        result[1] = 0xCC;
        encrypted.CopyTo(result, 2);
        return Convert.ToBase64String(result);
    }

    private static void Bytes(List<byte> target, ulong field, ReadOnlySpan<byte> value)
    {
        WriteVarint(target, field * 8 + 2);
        WriteVarint(target, (ulong)value.Length);
        target.AddRange(value);
    }

    private static void Varint(List<byte> target, ulong field, ulong value)
    {
        WriteVarint(target, field * 8);
        WriteVarint(target, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte RotateLeft(byte value, int offset) =>
        (byte)((value << offset) | (value >> (8 - offset)));

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
        byte[] array4 = new byte[32];
        Sm3(input, array4);
        return array4;
    }

    private static void Sm3(ReadOnlySpan<byte> input, Span<byte> destination)
    {
        ulong value = (ulong)input.Length * 8uL;
        int i;
        for (i = input.Length + 1; i % 64 != 56; i++) { }
        byte[]? rented = null;
        Span<byte> array =
            i + 8 <= 512
                ? stackalloc byte[i + 8]
                : (rented = ArrayPool<byte>.Shared.Rent(i + 8)).AsSpan(0, i + 8);
        try
        {
            array.Clear();
            input.CopyTo(array);
            array[input.Length] = 128;
            BinaryPrimitives.WriteUInt64BigEndian(array.Slice(i, 8), value);
            Span<uint> array2 =
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
            Span<uint> array3 = stackalloc uint[68];
            for (int j = 0; j < array.Length; j += 64)
            {
                array3.Clear();
                for (int k = 0; k < 16; k++)
                {
                    array3[k] = BinaryPrimitives.ReadUInt32BigEndian(array.Slice(j + k * 4, 4));
                }
                for (int l = 16; l < array3.Length; l++)
                {
                    uint num =
                        array3[l - 16]
                        ^ array3[l - 9]
                        ^ BitOperations.RotateLeft(array3[l - 3], 15);
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
                    uint value2 = m < 16 ? 2043430169u : 2055708042u;
                    uint num18 =
                        m < 16
                            ? num10 ^ num11 ^ num12
                            : (num10 & num11) | (num10 & num12) | (num11 & num12);
                    uint num19 =
                        m < 16 ? num14 ^ num15 ^ num16 : (num14 & num15) | (~num14 & num16);
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
            for (int n = 0; n < array2.Length; n++)
            {
                BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(n * 4, 4), array2[n]);
            }
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static byte[] Pad(ReadOnlySpan<byte> data)
    {
        int num = 16 - data.Length % 16;
        byte[] array = new byte[data.Length + num];
        data.CopyTo(array);
        array.AsSpan(data.Length).Fill((byte)num);
        return array;
    }
}
