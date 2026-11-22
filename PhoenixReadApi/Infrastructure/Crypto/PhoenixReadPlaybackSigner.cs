using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.IO.Compression;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>红果短剧 V5 播放协议专用签名与加密器（v04.10.03）。</summary>
internal static class PhoenixReadPlaybackSigner
{
    private const string V5License = "646637794";
    private const string AppId = "8662";

    private static readonly byte[] V5Magic = Convert.FromHexString(
        "2d4b4fca49750d433fb5ae2c226dcc56"
    );

    private static readonly byte[] V5SignKey = Convert.FromHexString(
        "b23c2a470a20d105dc3f8c4438f8272ed46d6c89ffdcdedd5108698d26e9489d"
    );

    private static readonly byte[] V5Sbox = Convert.FromHexString(
        "53290fe9e51f316f90f74a7e034d367784ab49237cd0135cdc632c322fd3be0e"
            + "2624a417ecd9151a7ff64f60730a87f9445ec6ada2f865d22b696c4188f24c0d"
            + "f37b75510b939f6bc2fe6883e68bc8c722181ba0bdb08c9871b50c9a16b46eea"
            + "8eed20ba4b8a14e0fcf1468dbbef05a93e5a9d7dcf06898635c1fd99f5de287a"
            + "80c594daaab19b6279fbd75070cb52eb2555c456e39e7839853874e2f0013ac0"
            + "b64742d8ca10e4b79c955de8b843ce4ed464a6a72ed508091c92e7b3eedd1e1d"
            + "d12afac321618fa3d65f9167c982f43d66a53c6a1997585b346dbf9672ff37cd"
            + "bc073b2dae11453304df00815476b2a13fccdb30b957a8acafe1274002485912"
            + "38922563e36406d37a24100b7902a028044e218453c2a399199c9d13dc40c55b"
            + "a67e502273850ebdc1a2e2188b8f2d9e3cb909b8c46a2a9f4a6dd93007eafa65"
            + "b7b3a881aa908cc74f0ad76bbbb58617c37012ca05a469fd44e62eecf1e4272c"
            + "e860c0ae5eadf99b20d8f0234174006f3f110d7657d01645d4eff8f71471cbfe"
            + "1cb2f52648727bf3c9de55153e031b4d61757db63dbf7782eeafe9018a889136"
            + "2fb14759ac32bab03b349aeb5a377c5156be4c8dfbe13a684bda39cf836697c8"
            + "94545fe0f2491acea9fc6252f4ff1e0fe72bd2df6e67a7f635e5bcc6d1a5dd95"
            + "1d89960887a18eb40c785cdb1f80ed42299333ab98cdd67f316cd54346585dcc"
            + "d759fb722789b08807cc0fda293f7e5e1819bb0353517020b31d3d00374952be"
            + "a2c421e862040a9ec892a05b266b7a0d0cc316f483b81af39715657ffe32ad8d"
            + "e0c13077646035236ee5363e5f900e9a7cf850a81cd9960b6df73317226f99ea"
            + "a6fff15cec34acb98246639b918ab2851466a7cdfc4ad1b52c2ae95a58d3734e"
            + "47ddd5b7560193f644ef9c408b7dee39b4d6aabf4de761e3252d4c7ba3843187"
            + "745d55e6a5db05d42b45caed43c0c5d0b64bfa9d94a4a9d8f2f0e1a108dcfd28"
            + "8fd2bc1fc7bd675412138169cb09249875ba3b68cfb11180afae104fc9789571"
            + "c6dfce02413886796ae21e3c488cc21b9f423a066c2eabf98e2fdeeb5776e4f5"
            + "d09a515ba58cbbab3e37b09fd4f6d9244df80512e84cd7956be301cf64f1429d"
            + "58577cc241f3a2c655144f81aaf95a4e108da894b38970561ae9002619fbbac9"
            + "1fa922ed202bfc62cb08e0de2a7f3631435f8b47c8a3c563b4db4071ce74c46d"
            + "a6fa696a9e282d1e825d663b93bfd56c48dce1884bb10fccea063929f746443f"
            + "d830a4232e53da21a1f2d1bdfebc3a02c10a1b8ad31d04fd616590d272ac737e"
            + "98eb33af60528716bee78ec0ee8699031177277d9cff32542c67caf5f4459168"
            + "1335ae0e178f3409a7803db83892dde5b9b7f0c750e24aef077badd678a09b85"
            + "0b84ec1597e4963cb5e675df1c0d6fb618496ecd76c35c798325590c2fb27a5e"
    );

    private static readonly uint[] HashTable =
    [
        0xEBB64FAF,
        0x07AADCC2,
        0xCF3187BF,
        0xE01138FF,
        0x6D0BFCFF,
        0x5A30A3BE,
        0xB41AD638,
        0x34180EB8,
        0xF233EB6F,
        0xB1A584CC,
        0xCCC30DC7,
        0x47D1DB51,
        0xD55653DE,
        0x70A84FA1,
        0x57473C12,
        0xF76F0288,
        0x2C077F0A,
        0xDA0DCAD0,
        0xFBB86F6C,
        0xFDC4CF00,
        0x688A020D,
        0xE676C6A6,
        0x8CD6338B,
        0x1A3C8D0E,
        0xCCE8B06B,
        0x6AD0ED0B,
        0xA0522717,
        0xDC71AC83,
        0x2285DB71,
        0xD5B4DDA6,
        0x736F8650,
        0x6560306C,
        0x617CE2A6,
        0xE423417E,
        0x0A40E143,
        0x544E4032,
        0x88DFFB2A,
        0x716C1AE0,
        0x4C467A88,
        0x05B23BB3,
        0xE1D0B866,
        0xBAA3DCB8,
        0xAE3374D3,
        0xC3381A50,
        0x1702F75B,
        0xFE6DA368,
        0xF0B4CF48,
        0x4E0FFBB8,
        0x72AAD10D,
        0x26C53A3D,
        0xF2BCE0F6,
        0xB4557581,
        0x4A257FDD,
        0x8C3182A2,
        0xAB0B3B86,
        0x3D5DFB14,
        0x4F103634,
        0xD37B52D7,
        0x444EFF16,
        0xEB0A33D1,
        0x6CA86F6E,
        0x00284BA7,
        0x08387CFA,
        0x5FB37586,
    ];

    private static readonly int[][] Con =
    [
        [2, 0, 3, 1],
        [1, 3, 2, 0],
        [3, 2, 1, 0],
        [2, 3, 1, 0],
    ];

    private static readonly int[][] Order =
    [
        [0, 9, 14, 15, 4, 13, 2, 7, 8, 1, 6, 3, 12, 5, 10, 11],
        [0, 9, 14, 11, 4, 13, 2, 3, 8, 1, 6, 15, 12, 5, 10, 7],
        [0, 9, 14, 3, 4, 13, 2, 15, 8, 1, 6, 11, 12, 5, 10, 7],
        [0, 9, 14, 15, 4, 13, 2, 3, 8, 1, 6, 11, 12, 5, 10, 7],
    ];

    private static readonly uint[] InitialWords = [0xBBB5DE72, 0xD4638D5B, 0x92A5F73B, 0x89F8A1C9];
    private static readonly int[] Wire = [6, 5, 3, 4, 2, 7, 1, 0];
    private static readonly int[] Compact = [1, 7, 3, 6, 2, 0, 4, 5];

    private static readonly int[] V5Chosen = [96, 1, 3, 4, 0, 6, 5, 2, 0, 7, 7];
    private static readonly uint[] V5Initials =
    [
        0x7ABA4FC8,
        0x67166507,
        0x6403FA00,
        0x340F512F,
        984304912,
        3005047866,
        2874125293,
        2152413264,
    ];

    private static readonly byte[] StaticEncKey = MD5.HashData(V5SignKey.AsSpan(0, 16));
    private static readonly byte[] StaticEncIv = MD5.HashData(V5SignKey.AsSpan(16, 16));

    private static readonly byte[] StaticSub15 = [0x08, 0x02, 0x10, 0x02, 0x18, 0x02, 0x28, 0x02];

    private static readonly byte[] StaticTag24Json =
        "{\"cmr\":16777216,\"cmr2\":16777216,\"un_h\":11,\"vpn\":0,\"kd\":0,\"dyn\":\"\",\"do\":1,\"tk\":true}"u8.ToArray();
    private static readonly byte[] StaticNoneStr = "none"u8.ToArray();
    private static readonly byte[] StaticV041003Str = "v04.10.03-ml-android"u8.ToArray();
    private static readonly byte[] StaticV5LicenseStr = "646637794"u8.ToArray();
    private static readonly byte[] StaticVersionStr = "7.4.3.32"u8.ToArray();
    private static readonly byte[] StaticAppIdStr = "8662"u8.ToArray();
    private static readonly byte[] StaticBugfixStr = "v04.10.03.04-bugfix"u8.ToArray();
    private static readonly byte[] StaticChannelStr = "vivo_8662_64"u8.ToArray();
    private static readonly byte[] StaticNoPermStr = "!noperm!"u8.ToArray();
    private static readonly byte[] StaticTzStr = "Asia/Shanghai,8"u8.ToArray();
    private static readonly byte[] StaticZhCnStr = "zh_CN"u8.ToArray();
    private static readonly byte[] StaticHonorStr = "HONOR"u8.ToArray();
    private static readonly byte[] StaticOsVersionStr = "17"u8.ToArray();
    private static readonly byte[] StaticDeviceTypeStr = "WKL-AN20"u8.ToArray();

    // 进程级稳定的 Nonce 与启动时间（与官方客户端行为对齐，防止被服务端风控判空）
    private static readonly string ProcessNonceBase64 = Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(18)
    )[..24];
    private static readonly long ProcessStartMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    internal static byte[] CompressJavaGzip(byte[] data)
    {
        using MemoryStream ms = new(data.Length / 2 + 64);
        using (GZipStream gz = new(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            gz.Write(data, 0, data.Length);
        }
        byte[] raw = ms.ToArray();
        if (raw.Length > 9)
        {
            raw.AsSpan(4, 6).Clear();
        }
        return raw;
    }

    internal static string BuildPlaybackUrlPrefix(
        string baseUri,
        string deviceId,
        string iid,
        string cdid
    )
    {
        StringBuilder sb = new(512);
        sb.Append(baseUri)
            .Append("?aid=")
            .Append(AppId)
            .Append("&app_name=novelread")
            .Append("&version_code=74332")
            .Append("&version_name=7.4.3.32")
            .Append("&channel=vivo_8662_64")
            .Append("&device_platform=android")
            .Append("&device_type=WKL-AN20")
            .Append("&os_version=17")
            .Append("&update_version_code=74332")
            .Append("&iid=")
            .Append(Uri.EscapeDataString(iid))
            .Append("&cdid=")
            .Append(Uri.EscapeDataString(cdid))
            .Append("&device_id=")
            .Append(Uri.EscapeDataString(deviceId))
            .Append("&_rticket=");
        return sb.ToString();
    }

    internal static string BuildPlaybackUrl(
        string baseUri,
        string deviceId,
        string iid,
        string cdid,
        long ticketMs
    ) => $"{BuildPlaybackUrlPrefix(baseUri, deviceId, iid, cdid)}{ticketMs}";

    internal static (string Khronos, string Helios, string Medusa, string Stub) SignV5(
        string fullUrl,
        byte[] gzipBody,
        uint timestamp,
        string deviceId
    )
    {
        string khronos = timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string helios = GenerateHelios(timestamp);
        string medusa = GenerateMedusa(fullUrl, gzipBody, timestamp, deviceId);
        string stub = Convert.ToHexString(MD5.HashData(gzipBody));
        return (khronos, helios, medusa, stub);
    }

    internal static string GenerateHelios(uint timestamp, byte[]? randomBytes = null)
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

    internal static byte[] EncodeDeviceInfo(
        string deviceId,
        string nonceB64,
        long startMs,
        string osVersion,
        string deviceType
    ) =>
        EncodeProto(
            (1, ProtoVal.Sint(1)),
            (2, ProtoVal.Sint(2)),
            (3, ProtoVal.Bytes(StaticAppIdStr)),
            (4, ProtoVal.Str(deviceId)),
            (5, ProtoVal.Str(nonceB64)),
            (6, ProtoVal.Bytes(StaticNoPermStr)),
            (12, ProtoVal.Bytes(StaticTzStr)),
            (13, ProtoVal.Bytes(StaticZhCnStr)),
            (22, ProtoVal.Bytes(StaticOsVersionStr)),
            (25, ProtoVal.Sint(startMs)),
            (30, ProtoVal.Bytes(StaticDeviceTypeStr)),
            (31, ProtoVal.Bytes(StaticHonorStr)),
            (32, ProtoVal.Bytes(StaticDeviceTypeStr)),
            (34, ProtoVal.Bytes(StaticHonorStr))
        );

    internal static string GenerateMedusa(
        string fullUrl,
        byte[] gzipBody,
        uint timestamp,
        string deviceId
    ) =>
        GenerateMedusaCore(
            fullUrl,
            gzipBody,
            timestamp,
            deviceId,
            ProcessNonceBase64,
            ProcessStartMs,
            (uint)RandomNumberGenerator.GetInt32(0, int.MaxValue),
            BinaryPrimitives.ReadUInt32LittleEndian(RandomNumberGenerator.GetBytes(4)),
            BinaryPrimitives.ReadUInt32LittleEndian(RandomNumberGenerator.GetBytes(4))
        );

    internal static string GenerateMedusaCore(
        string fullUrl,
        byte[] gzipBody,
        uint timestamp,
        string deviceId,
        string nonceB64,
        long startMs,
        uint msgRandom,
        uint nonce,
        uint packRand
    )
    {
        int qIdx = fullUrl.IndexOf('?');
        ReadOnlySpan<char> querySpan = qIdx >= 0 ? fullUrl.AsSpan(qIdx + 1) : [];
        int maxQueryBytes = Encoding.UTF8.GetMaxByteCount(querySpan.Length);
        byte[]? rentedQuery = null;
        Span<byte> queryBytes =
            maxQueryBytes <= 512
                ? stackalloc byte[maxQueryBytes]
                : (rentedQuery = ArrayPool<byte>.Shared.Rent(maxQueryBytes));
        int queryWritten = Encoding.UTF8.GetBytes(querySpan, queryBytes);

        Span<byte> querySm3 = stackalloc byte[32];
        try
        {
            PhoenixReadRequestSigner.Sm3(queryBytes[..queryWritten], querySm3);
        }
        finally
        {
            if (rentedQuery is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedQuery);
            }
        }

        Span<byte> bodyMd5 = stackalloc byte[16];
        if (gzipBody.Length > 0)
        {
            MD5.HashData(gzipBody, bodyMd5);
        }

        byte[] f13 = HashF13V5(querySm3, bodyMd5, timestamp);

        byte[] flags = new byte[8];
        const string osVersion = "17";
        const string deviceType = "WKL-AN20";

        // Protobuf deviceInfo
        byte[] deviceInfo = EncodeDeviceInfo(deviceId, nonceB64, startMs, osVersion, deviceType);

        // Protobuf env
        byte[] env = EncodeProto(
            (6, ProtoVal.Bytes(StaticBugfixStr)),
            (8, ProtoVal.Bytes(StaticChannelStr)),
            (12, ProtoVal.Bytes(deviceInfo)),
            (14, ProtoVal.Bytes(StaticVersionStr))
        );

        // Protobuf message
        byte[] message = EncodeProto(
            (1, ProtoVal.Bytes(V5Magic)),
            (2, ProtoVal.Sint(5)),
            (3, ProtoVal.Sint(msgRandom)),
            (4, ProtoVal.Bytes(StaticAppIdStr)),
            (5, ProtoVal.Str(deviceId)),
            (6, ProtoVal.Bytes(StaticV5LicenseStr)),
            (7, ProtoVal.Bytes(StaticVersionStr)),
            (8, ProtoVal.Bytes(StaticV041003Str)),
            (9, ProtoVal.Sint(0x040A0300)),
            (10, ProtoVal.Bytes(flags)),
            (12, ProtoVal.Sint(timestamp)),
            (13, ProtoVal.Bytes(f13)),
            (14, ProtoVal.Bytes(querySm3[..6].ToArray())),
            (15, ProtoVal.Bytes(StaticSub15)),
            (17, ProtoVal.Sint(timestamp)),
            (20, ProtoVal.Bytes(StaticNoneStr)),
            (21, ProtoVal.Sint(377)),
            (23, ProtoVal.Bytes(env)),
            (24, ProtoVal.Bytes(StaticTag24Json))
        );

        Span<byte> sm3KeyInput = stackalloc byte[68];
        V5SignKey.CopyTo(sm3KeyInput);
        BinaryPrimitives.WriteUInt32LittleEndian(sm3KeyInput.Slice(32, 4), nonce);
        V5SignKey.CopyTo(sm3KeyInput.Slice(36, 32));

        Span<byte> keySm3 = stackalloc byte[32];
        PhoenixReadRequestSigner.Sm3(sm3KeyInput, keySm3);

        uint high = (nonce >> 16) & 255;
        uint seedVal = ~(((high << 11) | (nonce >> 24)) ^ (high >> 5) ^ high);
        Span<byte> seedBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(seedBytes, seedVal);

        byte[] combined = new byte[flags.Length + message.Length];
        XmXorCore(message, keySm3, combined.AsSpan(flags.Length));
        Array.Reverse(combined);

        for (int i = 0; i < combined.Length; i++)
        {
            combined[i] ^= seedBytes[(~i) & 3];
        }

        uint check = 0x18000001u | ((querySm3[0] & 63u) << 14) | ((f13[0] & 63u) << 8);

        int packedLen = 1 + 4 + 4 + combined.Length + 2;
        byte[]? rentedPacked = null;
        Span<byte> packed =
            packedLen <= 512
                ? stackalloc byte[packedLen]
                : (rentedPacked = ArrayPool<byte>.Shared.Rent(packedLen));
        try
        {
            packed[0] = 0xCA;
            BinaryPrimitives.WriteUInt32LittleEndian(packed.Slice(1, 4), packRand);
            BinaryPrimitives.WriteUInt32LittleEndian(packed.Slice(5, 4), check);
            combined.CopyTo(packed[9..]);
            packed[packedLen - 2] = (byte)((nonce >> 16) & 0xFF);
            packed[packedLen - 1] = (byte)((nonce >> 24) & 0xFF);

            int encLen = 1 + packedLen;
            byte[] result = new byte[24 + encLen];
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(0, 4), 5);
            V5Magic.CopyTo(result, 4);

            for (int i = 0; i < 20; i++)
            {
                result[i] ^= (byte)(timestamp >> ((i & 3) * 8));
            }

            result[20] = (byte)(nonce & 0xFF);
            result[21] = (byte)((nonce >> 8) & 0xFF);
            result[22] = 0;
            result[23] = 1;

            AesV3EncryptCore(
                packed[..packedLen],
                StaticEncKey,
                StaticEncIv,
                timestamp,
                result.AsSpan(24, encLen)
            );

            return Convert.ToBase64String(result);
        }
        finally
        {
            if (rentedPacked is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedPacked);
            }
        }
    }

    internal static byte[] HashF13V5(byte[] querySm3, byte[] bodyMd5, uint timestamp) =>
        HashF13V5(querySm3.AsSpan(), bodyMd5.AsSpan(), timestamp);

    internal static byte[] HashF13V5(
        ReadOnlySpan<byte> querySm3,
        ReadOnlySpan<byte> bodyMd5,
        uint timestamp
    )
    {
        byte[] f13Result = new byte[20];
        HashF13V5(querySm3, bodyMd5, timestamp, f13Result);
        return f13Result;
    }

    internal static void HashF13V5(
        ReadOnlySpan<byte> querySm3,
        ReadOnlySpan<byte> bodyMd5,
        uint timestamp,
        Span<byte> destination
    )
    {
        const uint ivV1 = 0x9D4A97C2u;
        uint countV1 = (ivV1 + timestamp + 1) & 255;
        uint countV2 = ivV1 + timestamp;

        Span<uint> tt = stackalloc uint[HashTable.Length];
        for (int i = 0; i < HashTable.Length; i++)
        {
            tt[i] = BitOperations.RotateRight(HashTable[i], (int)countV1);
        }

        int shift = (int)((countV2 + 2) & 7);
        Span<byte> pad =
        [
            (byte)(((0xFA | (0xFA << 8)) >> shift) & 0xFF),
            (byte)(((0x45 | (0x45 << 8)) >> shift) & 0xFF),
            (byte)(((0x61 | (0x61 << 8)) >> shift) & 0xFF),
            (byte)(((0xD7 | (0xD7 << 8)) >> shift) & 0xFF),
        ];
        Span<byte> data = stackalloc byte[64];
        querySm3.CopyTo(data);
        bodyMd5.CopyTo(data[32..]);
        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(48, 4), timestamp);
        pad.CopyTo(data[52..]);
        BinaryPrimitives.WriteUInt64BigEndian(data.Slice(56, 8), 0x1a0);

        Span<uint> di = stackalloc uint[128];
        for (int i = 0; i < 16; i++)
        {
            di[i] = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(i * 4, 4));
        }

        for (int i = 0; i < 112; i++)
        {
            uint x = di[i + 1];
            uint y = di[i + 14];
            uint xRot =
                BitOperations.RotateLeft(x, 14) ^ BitOperations.RotateLeft(x, 25) ^ (x >> 3);
            uint yRot =
                BitOperations.RotateLeft(y, 13) ^ BitOperations.RotateLeft(y, 15) ^ (y >> 10);
            di[16 + i] = di[i] + di[i + 9] + xRot + yRot;
        }

        Span<uint> initial = stackalloc uint[8];
        for (int i = 0; i < 8; i++)
        {
            initial[i] = BitOperations.RotateRight(V5Initials[i], (int)(countV2 & 31));
        }

        Span<uint> d = stackalloc uint[8];
        initial.CopyTo(d);
        for (int i = 0; i < V5Chosen[0]; i++)
        {
            int b = unchecked((int)ivV1) + i;
            uint n1 = ((d[V5Chosen[3]] ^ d[V5Chosen[4]]) & d[V5Chosen[1]]) ^ d[V5Chosen[3]];
            uint n2 =
                BitOperations.RotateLeft(d[V5Chosen[1]], 26)
                ^ BitOperations.RotateLeft(d[V5Chosen[1]], 21)
                ^ BitOperations.RotateLeft(d[V5Chosen[1]], 7);
            uint n4 = unchecked(
                (uint)((int)di[b & 127] + (int)n1 + (int)n2 + (int)tt[b & 63] + (int)d[V5Chosen[5]])
            );
            uint n5 =
                BitOperations.RotateLeft(d[V5Chosen[2]], 30)
                ^ BitOperations.RotateLeft(d[V5Chosen[2]], 19)
                ^ BitOperations.RotateLeft(d[V5Chosen[2]], 10);
            uint n6 =
                (d[V5Chosen[2]] & d[V5Chosen[6]])
                | ((d[V5Chosen[2]] | d[V5Chosen[6]]) & d[V5Chosen[7]]);

            uint old = d[V5Chosen[9]];
            uint last = d[7];
            for (int k = 7; k > 0; k--)
            {
                d[k] = d[k - 1];
            }
            d[0] = last;
            d[V5Chosen[10]] = unchecked((uint)((int)n5 + (int)n6 + (int)n4));
            d[V5Chosen[8]] = unchecked((uint)((int)old + (int)n4));
        }

        Span<byte> ret = stackalloc byte[32];
        for (int i = 0; i < 8; i++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(ret.Slice(i * 4, 4), d[i] + initial[i]);
        }

        Span<byte> folded = stackalloc byte[16];
        for (int i = 0; i < 16; i++)
        {
            folded[i] = (byte)(ret[i] ^ ret[16 + i]);
        }

        folded.CopyTo(destination);
        uint md5Check = SumMd5(folded) | 0x20u;
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(16, 4), md5Check);
    }

    private static uint SumMd5(ReadOnlySpan<byte> data)
    {
        uint check = 0x20220420u;
        for (int i = 0; i < 12; i++)
        {
            uint temp = (check >> ((i & 1) == 0 ? 3 : 5)) ^ check;
            if ((i & 1) == 0)
            {
                check = data[i] ^ (check << 7);
            }
            else
            {
                check = ~(data[i] | (check << 11));
            }
            check ^= temp;
        }
        return (check | 4u) ^ 0x1000000u;
    }

    internal static byte[] XmXor(byte[] data, byte[] key)
    {
        byte[] encoded = new byte[data.Length];
        XmXorCore(data, key, encoded);
        return encoded;
    }

    private static void XmXorCore(
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> key,
        Span<byte> encoded
    )
    {
        int n = data.Length;
        for (int i = 0; i < n; i++)
        {
            int at = (i * 4) & 28;
            uint d0 = key[at];
            uint d1 = key[at + 1];
            uint v = data[i];
            uint d2 = (((v << 4) | (v >> 4)) & 255) + d0;
            d2 = (~d2) ^ d1;
            uint m = d2 & 255;
            d2 = ((m << 3) | (m >> 5)) & 255;
            d2 = (d2 + d1) & 255;
            d2 = (d2 ^ d0) & 255;
            encoded[n - i - 1] = (byte)(~d2);
        }

        byte last = (byte)(encoded[^1] ^ encoded[^2]);
        byte first = encoded[0];
        encoded[0] = (byte)((~last) + first);
        encoded[1] = (byte)((encoded[0] ^ encoded[^1] ^ 254) + encoded[1]);
        byte t = (byte)(((encoded[1] << 3) | (encoded[1] >> 5)) & 255);
        encoded[2] = (byte)(encoded[2] + (((last - first) ^ t ^ 2) & 255));

        for (int i = 0; i < n - 4; i++)
        {
            byte temp = (byte)(
                (((encoded[i + 2] << 3) | (encoded[i + 2] >> 5)) ^ encoded[i + 1] ^ (i + 3)) & 255
            );
            encoded[i + 3] = (byte)((~temp) + encoded[i + 3]);
        }

        encoded[^1] = (byte)(encoded[^1] ^ encoded[^2]);
        uint ssum = 0;
        for (int i = 1; i < n; i++)
        {
            ssum += encoded[i];
        }
        encoded[0] = (byte)((encoded[0] ^ encoded[1]) + ssum);
    }

    internal static byte[] AesV3Encrypt(byte[] source, byte[] key, byte[] iv, uint timestamp)
    {
        byte[] finalBytes = new byte[1 + source.Length];
        AesV3EncryptCore(source, key, iv, timestamp, finalBytes);
        return finalBytes;
    }

    private static void AesV3EncryptCore(
        ReadOnlySpan<byte> source,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv,
        uint timestamp,
        Span<byte> destination
    )
    {
        int word = (int)(timestamp & 3);
        ReadOnlySpan<byte> box = V5Sbox.AsSpan(word * 256, 256);
        ReadOnlySpan<int> con = Con[word];
        ReadOnlySpan<int> order = Order[word];
        uint initial = InitialWords[word];

        Span<byte> expanded = stackalloc byte[80];
        Span<byte> leInit = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(leInit, initial);
        for (int i = 0; i < 16; i++)
        {
            expanded[i] = (byte)(leInit[i % 4] ^ key[i % 16]);
        }

        int rounds = 8;
        for (int i = 4; i < 12; i++)
        {
            int at = 4 * (i - 1);
            byte a = expanded[at];
            byte b = expanded[at + 1];
            byte c = expanded[at + 2];
            byte d = expanded[at + 3];

            if ((i & 3) == 0)
            {
                byte t = (byte)(((initial >> (rounds & 24)) ^ box[b]) & 255);
                b = box[c];
                c = box[d];
                d = box[a];
                a = t;
            }
            rounds += 2;
            expanded[at + 4] = (byte)(a ^ expanded[at - 12]);
            expanded[at + 5] = (byte)(b ^ expanded[at - 11]);
            expanded[at + 6] = (byte)(c ^ expanded[at - 10]);
            expanded[at + 7] = (byte)(d ^ expanded[at - 9]);
        }

        static void Block(
            ReadOnlySpan<byte> input,
            ReadOnlySpan<byte> expanded,
            ReadOnlySpan<int> con,
            ReadOnlySpan<byte> box,
            ReadOnlySpan<int> order,
            Span<byte> output
        )
        {
            Span<byte> rows = stackalloc byte[16];
            Span<byte> before = stackalloc byte[16];
            input.CopyTo(rows);

            for (int i = 0; i < 4; i++)
            {
                int offI = i * 4;
                for (int j = 0; j < 4; j++)
                {
                    rows[offI + j] ^= expanded[offI + con[j]];
                }
            }

            for (int rnd = 1; rnd <= 2; rnd++)
            {
                for (int i = 0; i < 16; i++)
                {
                    rows[i] = box[rows[i]];
                }

                rows.CopyTo(before);
                for (int i = 0; i < 4; i++)
                {
                    int ci = con[i] * 4;
                    int ri = i * 4;
                    for (int j = 0; j < 4; j++)
                    {
                        rows[ri + j] = before[ci + j];
                    }
                }

                rows.CopyTo(before);
                for (int i = 0; i < 16; i++)
                {
                    rows[i] = before[order[i]];
                }

                if (rnd == 1)
                {
                    rows.CopyTo(before);
                    for (int i = 0; i < 4; i++)
                    {
                        int ri = i * 4;
                        for (int j = 0; j < 4; j++)
                        {
                            rows[ri + j] = before[ri + con[j]];
                        }
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        byte r0 = rows[i];
                        byte r1 = rows[4 + i];
                        byte r2 = rows[8 + i];
                        byte r3 = rows[12 + i];
                        byte t = (byte)(r0 ^ r1 ^ r2 ^ r3);
                        rows[i] ^= (byte)(t ^ Xt((byte)(r0 ^ r1)));
                        rows[4 + i] ^= (byte)(t ^ Xt((byte)(r1 ^ r2)));
                        rows[8 + i] ^= (byte)(t ^ Xt((byte)(r2 ^ r3)));
                        rows[12 + i] ^= (byte)(t ^ Xt((byte)(r3 ^ r0)));
                    }
                }

                int conOff = rnd * 16;
                for (int i = 0; i < 4; i++)
                {
                    int offI = conOff + i * 4;
                    for (int j = 0; j < 4; j++)
                    {
                        rows[i * 4 + j] ^= expanded[offI + con[j]];
                    }
                }
            }

            for (int i = 0; i < 4; i++)
            {
                int offI = 16 + i * 4;
                for (int j = 0; j < 4; j++)
                {
                    rows[i * 4 + j] ^= expanded[offI + j];
                }
            }

            rows.CopyTo(output);
        }

        static byte Xt(byte x) => (byte)(((x << 1) ^ ((x & 128) != 0 ? 0x1B : 0)) & 255);

        Span<byte> pt = stackalloc byte[32];
        for (int i = 0; i < 31; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                int at = i * 8 + j;
                byte srcByte = at < source.Length ? source[at] : (byte)0;
                int bit = (srcByte >> Wire[j]) & 1;
                pt[i] |= (byte)(bit << Compact[j]);
            }
        }
        pt[31] = 1;

        Span<byte> firstBlockInput = stackalloc byte[16];
        for (int i = 0; i < 16; i++)
        {
            firstBlockInput[i] = (byte)(pt[i] ^ iv[i]);
        }
        Span<byte> first = stackalloc byte[16];
        Block(firstBlockInput, expanded, con, box, order, first);

        Span<byte> secondBlockInput = stackalloc byte[16];
        for (int i = 0; i < 16; i++)
        {
            secondBlockInput[i] = (byte)(pt[16 + i] ^ first[i]);
        }
        Span<byte> second = stackalloc byte[16];
        Block(secondBlockInput, expanded, con, box, order, second);

        Span<byte> fullKey = stackalloc byte[32];
        first.CopyTo(fullKey);
        second.CopyTo(fullKey[16..]);

        destination[0] = fullKey[31];
        source.CopyTo(destination[1..]);

        for (int i = 0; i < 31; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                int at = 1 + i * 8 + j;
                if (at < destination.Length)
                {
                    byte cur = destination[at];
                    byte v = (byte)(
                        (cur & (~(1 << Wire[j]) & 0xFF))
                        | (((fullKey[i] >> Compact[j]) & 1) << Wire[j])
                    );
                    destination[at] = v;
                }
            }
        }
    }

    private readonly record struct ProtoVal(int Type, long IntVal, byte[]? ByteVal)
    {
        public static ProtoVal Sint(long val) => new(1, val, null);

        public static ProtoVal Str(string s) => new(2, 0, Encoding.UTF8.GetBytes(s));

        public static ProtoVal Bytes(byte[] b) => new(2, 0, b);
    }

    private static byte[] EncodeProto(params ReadOnlySpan<(int Tag, ProtoVal Val)> fields)
    {
        using MemoryStream ms = new(128);

        void WriteVarint(ulong v)
        {
            while (v >= 0x80)
            {
                ms.WriteByte((byte)((v & 0x7F) | 0x80));
                v >>= 7;
            }
            ms.WriteByte((byte)(v & 0x7F));
        }

        foreach ((int tag, ProtoVal val) in fields)
        {
            if (val.Type == 1) // sint (zigzag)
            {
                long n = val.IntVal;
                ulong z = n < 0 ? (ulong)(-n * 2 - 1) : (ulong)(n * 2);
                WriteVarint((ulong)(tag << 3));
                WriteVarint(z);
            }
            else if (val.Type == 2) // length-delimited
            {
                byte[] data = val.ByteVal ?? [];
                if (data.Length == 0)
                {
                    continue; // 空内容整字段不写
                }

                WriteVarint((ulong)((tag << 3) | 2));
                WriteVarint((ulong)data.Length);
                ms.Write(data, 0, data.Length);
            }
        }

        return ms.ToArray();
    }
}
