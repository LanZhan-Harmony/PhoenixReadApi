using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>
/// 六神算法之 X-Medusa 签名器：用于 V5 播放协议的 Protobuf、SM3、Simon 与 AES-128 综合签名混淆。
/// </summary>
internal static class XMedusaSigner
{
    private const string V5License = "646637794";
    private const string AppId = PhoenixReadDeviceProfile.AidString;

    private static readonly byte[] V5Magic = Convert.FromHexString(
        "2d4b4fca49750d433fb5ae2c226dcc56"
    );

    private static readonly byte[] V5SignKey = Convert.FromHexString(
        "b23c2a470a20d105dc3f8c4438f8272ed46d6c89ffdcdedd5108698d26e9489d"
    );

    private static readonly byte[] StaticEncKey = MD5.HashData(V5SignKey.AsSpan(0, 16));
    private static readonly byte[] StaticEncIv = MD5.HashData(V5SignKey.AsSpan(16, 16));
    private static readonly byte[] StaticFlags = Convert.FromHexString("0405000000000000");

    private static readonly byte[] StaticBugfixStr = "v04.10.03.04-bugfix"u8.ToArray();
    private static readonly byte[] StaticVersionMlStr = "v04.10.03-ml-android"u8.ToArray();
    private static readonly byte[] StaticNoneStr = "none"u8.ToArray();
    private static readonly byte[] StaticAppIdBytes = Encoding.UTF8.GetBytes(AppId);
    private static readonly byte[] StaticV5LicenseBytes = Encoding.UTF8.GetBytes(V5License);
    private static readonly byte[] StaticTag24Json =
        "{\"cmr\":16777216,\"cmr2\":16777216,\"un_h\":2398682137,\"vpn\":0,\"sts\":27,\"kd\":0,\"fkd\":1963693275,\"pd\":1442983616,\"lp\":\"2|520830913092|521433182354|2078052786776|1081701896\",\"fl\":\"0|0|2523408538|2586339478|997724116|0|0|0|0|606600138\",\"dyn\":\"\",\"do\":0,\"tk\":true}"u8.ToArray();

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

    // 进程级稳定的 Nonce 与启动时间（与官方客户端行为对齐，防止被服务端风控判空）
    private static readonly string ProcessNonceBase64 = Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(18)
    )[..24];
    private static readonly long ProcessStartMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private static readonly long s_processSeed = (long)RandomNumberGenerator.GetInt32(0, int.MaxValue);
    private static readonly long s_pid = 3000L + (s_processSeed % 25000L);
    private static readonly long s_ppid = 500L + ((s_processSeed / 25000L) % 700L);
    private static long s_counter = 20L + (s_processSeed % 100L);

    /// <summary>为 V5 播放请求生成 X-Medusa 签名。</summary>
    public static string SignV5(
        string fullUrl,
        byte[] gzipBody,
        uint timestamp,
        string deviceId,
        string? osVersion = null,
        string? deviceType = null,
        string? version = null,
        string? channel = null
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
            BinaryPrimitives.ReadUInt32LittleEndian(RandomNumberGenerator.GetBytes(4)),
            osVersion,
            deviceType,
            version,
            channel
        );

    /// <summary>V5 Medusa 核心算法（支持外部指定设备参数，便于黄金测试向量回归与统一设备配置）。</summary>
    public static string GenerateMedusaCore(
        string fullUrl,
        byte[] gzipBody,
        uint timestamp,
        string deviceId,
        string nonceB64,
        long startMs,
        uint msgRandom,
        uint nonce,
        uint packRand,
        string? osVersion = null,
        string? deviceType = null,
        string? version = null,
        string? channel = null
    )
    {
        string queryStr = fullUrl.Contains('?')
            ? fullUrl[(fullUrl.IndexOf('?') + 1)..]
            : string.Empty;

        string effectiveOs = osVersion
            ?? ExtractParam(queryStr, "os_version")
            ?? PhoenixReadDeviceProfile.OsVersion;
        string effectiveDevice = deviceType
            ?? ExtractParam(queryStr, "device_type")
            ?? PhoenixReadDeviceProfile.DeviceModel;
        string effectiveVersion = version
            ?? ExtractParam(queryStr, "version_name")
            ?? PhoenixReadDeviceProfile.VersionName;
        string effectiveChannel = channel
            ?? ExtractParam(queryStr, "channel")
            ?? PhoenixReadDeviceProfile.Channel;
        string effectiveDeviceId =
            !string.IsNullOrEmpty(deviceId) && deviceId != "0"
                ? deviceId
                : ExtractParam(queryStr, "device_id") ?? deviceId;

        byte[] queryBytes = Encoding.UTF8.GetBytes(queryStr);
        byte[] querySm3 = new byte[32];
        PhoenixReadRequestSigner.Sm3(queryBytes, querySm3);

        byte[] bodyMd5 = gzipBody.Length > 0 ? MD5.HashData(gzipBody) : new byte[16];
        byte[] f13 = HashF13V5(querySm3, bodyMd5, timestamp);

        byte[] flags = StaticFlags;
        long started = startMs <= 0 ? ProcessStartMs : startMs;
        long nowMs = (long)timestamp * 1000 + (msgRandom % 1000);
        long uptime = Math.Max(1L, (long)timestamp - (started / 1000L));

        // Protobuf deviceInfo (real device layout)
        byte[] deviceInfo = EncodeDeviceInfo(
            effectiveDeviceId,
            started,
            nowMs,
            effectiveOs,
            effectiveDevice,
            PhoenixReadDeviceProfile.DeviceBrand
        );

        // Protobuf env
        byte[] env = EncodeProto(
            (1, ProtoVal.Sint(uptime)),
            (6, ProtoVal.Bytes(StaticBugfixStr)),
            (7, ProtoVal.Sint(s_pid)),
            (8, ProtoVal.Str(effectiveChannel)),
            (12, ProtoVal.Bytes(deviceInfo)),
            (13, ProtoVal.Bytes([])),
            (14, ProtoVal.Str(effectiveVersion)),
            (15, ProtoVal.Sint(s_ppid)),
            (18, ProtoVal.Sint((long)timestamp + 766353820L)),
            (19, ProtoVal.Sint((long)timestamp + 16L)),
            (20, ProtoVal.Sint((long)timestamp - 1072050540L))
        );

        long count = Interlocked.Increment(ref s_counter);
        byte[] sub15 = EncodeProto(
            (1, ProtoVal.Sint(count)),
            (2, ProtoVal.Sint(5)),
            (3, ProtoVal.Sint(694367)),
            (5, ProtoVal.Sint(694367))
        );

        // Protobuf message
        byte[] message = EncodeProto(
            (1, ProtoVal.Bytes(V5Magic)),
            (2, ProtoVal.Sint(5)),
            (3, ProtoVal.Sint(msgRandom)),
            (4, ProtoVal.Bytes(StaticAppIdBytes)),
            (5, ProtoVal.Str(effectiveDeviceId)),
            (6, ProtoVal.Bytes(StaticV5LicenseBytes)),
            (7, ProtoVal.Str(effectiveVersion)),
            (8, ProtoVal.Bytes(StaticVersionMlStr)),
            (9, ProtoVal.Sint(0x040A0300)),
            (10, ProtoVal.Bytes(flags)),
            (12, ProtoVal.Sint(timestamp)),
            (13, ProtoVal.Bytes(f13)),
            (14, ProtoVal.Bytes(querySm3[..6])),
            (15, ProtoVal.Bytes(sub15)),
            (17, ProtoVal.Sint(timestamp)),
            (20, ProtoVal.Bytes(StaticNoneStr)),
            (21, ProtoVal.Sint(369)),
            (23, ProtoVal.Bytes(env)),
            (24, ProtoVal.Bytes(StaticTag24Json))
        );

        Span<byte> nonceLe = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(nonceLe, nonce);

        Span<byte> sm3KeyInput = stackalloc byte[V5SignKey.Length + 4 + V5SignKey.Length];
        V5SignKey.CopyTo(sm3KeyInput);
        nonceLe.CopyTo(sm3KeyInput[V5SignKey.Length..]);
        V5SignKey.CopyTo(sm3KeyInput[(V5SignKey.Length + 4)..]);

        Span<byte> keySm3 = stackalloc byte[32];
        PhoenixReadRequestSigner.Sm3(sm3KeyInput, keySm3);

        uint high = (nonce >> 16) & 255;
        uint seedVal = ~(((high << 11) | (nonce >> 24)) ^ (high >> 5) ^ high);
        Span<byte> seedBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(seedBytes, seedVal);

        byte[] xorTransformed = XmXor(message, keySm3);
        byte[] combined = new byte[flags.Length + xorTransformed.Length];
        flags.CopyTo(combined, 0);
        xorTransformed.CopyTo(combined, flags.Length);
        Array.Reverse(combined);

        for (int i = 0; i < combined.Length; i++)
        {
            combined[i] ^= seedBytes[(~i) & 3];
        }

        uint check = 0x18000001u | ((querySm3[0] & 63u) << 14) | ((f13[0] & 63u) << 8);

        byte[] packed = new byte[1 + 4 + 4 + combined.Length + 2];
        packed[0] = 0xCA;
        BinaryPrimitives.WriteUInt32LittleEndian(packed.AsSpan(1, 4), packRand);
        BinaryPrimitives.WriteUInt32LittleEndian(packed.AsSpan(5, 4), check);
        combined.CopyTo(packed, 9);
        packed[^2] = (byte)((nonce >> 16) & 0xFF);
        packed[^1] = (byte)((nonce >> 24) & 0xFF);

        byte[] encrypted = AesV3Encrypt(packed, StaticEncKey, StaticEncIv, timestamp);

        byte[] prefix = new byte[4 + V5Magic.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(prefix.AsSpan(0, 4), 5);
        V5Magic.CopyTo(prefix, 4);

        for (int i = 0; i < prefix.Length; i++)
        {
            prefix[i] ^= (byte)(timestamp >> ((i & 3) * 8));
        }

        byte[] result = new byte[prefix.Length + 4 + encrypted.Length];
        prefix.CopyTo(result, 0);
        result[prefix.Length] = (byte)(nonce & 0xFF);
        result[prefix.Length + 1] = (byte)((nonce >> 8) & 0xFF);
        result[prefix.Length + 2] = 0;
        result[prefix.Length + 3] = 1;
        encrypted.CopyTo(result, prefix.Length + 4);

        return Convert.ToBase64String(result);
    }

    internal static string MedusaV5Token(string nonce)
    {
        if (nonce.Length == 25 && nonce[0] == 'A')
        {
            return nonce;
        }
        if (nonce.Length >= 24)
        {
            return "A" + nonce[..24];
        }
        return "A" + nonce + new string('A', 24 - nonce.Length);
    }

    private static void DeriveDeviceMetrics(
        string deviceId,
        out float storage,
        out float freeStorage,
        out float memory,
        out float freeMemory,
        out long installMillis
    )
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("medusa-device\0" + deviceId));
        ulong Pick(int idx, ulong mod) => (ulong)BinaryPrimitives.ReadUInt32LittleEndian(hash.AsSpan(idx * 4, 4)) % mod;

        float[] storageOptions = [106.83f, 108.57f, 229.36f, 233.91f, 487.62f];
        storage = storageOptions[(int)Pick(0, 5)] + (float)Pick(1, 400) / 1000f;

        float[] memoryOptions = [5.37f, 7.42f, 11.18f, 15.07f];
        memory = memoryOptions[(int)Pick(2, 4)] + (float)Pick(3, 90) / 1000f;

        freeStorage = storage * (0.25f + (float)Pick(5, 500) / 1000f);
        freeMemory = 0.35f + (float)Pick(6, 900) / 1000f;

        const long DayMs = 86_400_000L;
        const long Anchor = 1795440816000L;
        installMillis = Anchor - DayMs - (long)Pick(4, 20 * DayMs);
    }

    internal static byte[] EncodeDeviceInfo(
        string deviceId,
        string nonceB64,
        long startMs,
        string osVersion,
        string deviceType
    ) => EncodeDeviceInfo(deviceId, startMs, startMs, osVersion, deviceType, PhoenixReadDeviceProfile.DeviceBrand);

    internal static byte[] EncodeDeviceInfo(
        string deviceId,
        string secToken,
        long startMs,
        long nowMs,
        string osVersion,
        string deviceType,
        string? deviceBrand = null
    ) => EncodeDeviceInfo(deviceId, startMs, nowMs, osVersion, deviceType, deviceBrand);

    internal static byte[] EncodeDeviceInfo(
        string deviceId,
        long launchMillis,
        long nowMillis,
        string osVersion,
        string deviceType,
        string? deviceBrand = null
    )
    {
        string brand = string.IsNullOrEmpty(deviceBrand) ? PhoenixReadDeviceProfile.DeviceBrand : deviceBrand;
        string model = string.IsNullOrEmpty(deviceType) ? PhoenixReadDeviceProfile.DeviceModel : deviceType;
        string os = string.IsNullOrEmpty(osVersion) ? PhoenixReadDeviceProfile.OsVersion : osVersion;

        DeriveDeviceMetrics(
            deviceId,
            out float storage,
            out float freeStorage,
            out float memory,
            out float freeMemory,
            out long installMillis
        );

        const string notSet = "!notset!";
        const string noPerm = "!noperm!";
        const int noPermInt = -888888;

        return EncodeProto(
            (1, ProtoVal.Sint(1)),
            (3, ProtoVal.Str(AppId)),
            (4, ProtoVal.Str(deviceId)),
            (5, ProtoVal.Str(notSet)),
            (6, ProtoVal.Str(noPerm)),
            (7, ProtoVal.Sint(noPermInt)),
            (8, ProtoVal.Sint(noPermInt)),
            (9, ProtoVal.Sint(2)),
            (10, ProtoVal.Sint(noPermInt)),
            (11, ProtoVal.Str(notSet)),
            (12, ProtoVal.Str("Asia/Shanghai,8")),
            (13, ProtoVal.Str("zh_CN")),
            (14, ProtoVal.Sint(8)),
            (16, ProtoVal.Flt(storage)),
            (17, ProtoVal.Flt(freeStorage)),
            (18, ProtoVal.Flt(memory)),
            (19, ProtoVal.Flt(freeMemory)),
            (20, ProtoVal.Flt(storage * 2f)),
            (21, ProtoVal.Flt(freeStorage * 2f)),
            (22, ProtoVal.Str(os)),
            (23, ProtoVal.Sint(60)),
            (24, ProtoVal.Sint(10)),
            (25, ProtoVal.Sint(installMillis)),
            (26, ProtoVal.Sint(launchMillis + 540L)),
            (27, ProtoVal.Sint(installMillis)),
            (28, ProtoVal.Sint(nowMillis)),
            (29, ProtoVal.Sint(1)),
            (30, ProtoVal.Str(model)),
            (31, ProtoVal.Str(brand)),
            (32, ProtoVal.Str(notSet)),
            (33, ProtoVal.Str(notSet)),
            (34, ProtoVal.Str(notSet)),
            (35, ProtoVal.Str(brand)),
            (36, ProtoVal.Str(notSet)),
            (38, ProtoVal.Sint(3)),
            (40, ProtoVal.Sint(launchMillis))
        );
    }

    internal static byte[] HashF13V5(
        ReadOnlySpan<byte> querySm3,
        ReadOnlySpan<byte> bodyMd5,
        uint timestamp
    )
    {
        const uint ivV1 = 0x9D4A97C2u;
        uint countV1 = (ivV1 + timestamp + 1) & 255;
        uint countV2 = ivV1 + timestamp;

        Span<uint> tt = stackalloc uint[64];
        for (int i = 0; i < 64; i++)
        {
            tt[i] = BitOperations.RotateRight(HashTable[i], (int)countV1);
        }

        int shift = (int)((countV2 + 2) & 7);
        Span<byte> pad = stackalloc byte[4]
        {
            (byte)(((0xFA | (0xFA << 8)) >> shift) & 0xFF),
            (byte)(((0x45 | (0x45 << 8)) >> shift) & 0xFF),
            (byte)(((0x61 | (0x61 << 8)) >> shift) & 0xFF),
            (byte)(((0xD7 | (0xD7 << 8)) >> shift) & 0xFF),
        };

        Span<byte> data = stackalloc byte[64];
        querySm3[..32].CopyTo(data[..32]);
        bodyMd5[..16].CopyTo(data.Slice(32, 16));
        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(48, 4), timestamp);
        pad.CopyTo(data[52..]);
        data[56] = 0;
        data[57] = 0;
        data[58] = 0;
        data[59] = 0;
        data[60] = 0;
        data[61] = 0;
        data[62] = 0x01;
        data[63] = 0xa0;

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

        byte[] f13Result = new byte[20];
        Span<byte> folded = f13Result.AsSpan(0, 16);
        for (int i = 0; i < 16; i++)
        {
            folded[i] = (byte)(ret[i] ^ ret[16 + i]);
        }

        HashF13Tail(folded, f13Result.AsSpan(16, 4));
        return f13Result;
    }

    internal static void HashF13Tail(ReadOnlySpan<byte> head, Span<byte> destination)
    {
        uint hash = 0x20220420u;
        for (int i = 0; i < 8; i++)
        {
            if ((i & 1) == 0)
            {
                hash = (hash << 7) ^ head[i] ^ (hash >> 3) ^ hash;
            }
            else
            {
                hash = ~(((hash << 11) | head[i]) ^ (hash >> 5) ^ hash);
            }
        }
        BinaryPrimitives.WriteUInt32LittleEndian(destination, hash);
        destination[0] &= 0xdb;
        destination[3] ^= 1;
    }

    internal static byte[] HashF13Tail(ReadOnlySpan<byte> head)
    {
        byte[] tail = new byte[4];
        HashF13Tail(head, tail);
        return tail;
    }

    internal static byte[] XmXor(byte[] data, ReadOnlySpan<byte> key)
    {
        int n = data.Length;
        byte[] encoded = new byte[n];
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
        return encoded;
    }

    internal static byte[] AesV3Encrypt(byte[] source, byte[] key, byte[] iv, uint timestamp)
    {
        int word = (int)(timestamp & 3);
        ReadOnlySpan<byte> box = V5Sbox.AsSpan(word * 256, 256);
        int[] con = Con[word];
        int[] order = Order[word];
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

        Span<byte> pt = stackalloc byte[32];
        pt.Clear();
        for (int i = 0; i < 31; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                int at = i * 8 + j;
                byte srcByte = (uint)at < (uint)source.Length ? source[at] : (byte)0;
                int bit = (srcByte >> Wire[j]) & 1;
                pt[i] |= (byte)(bit << Compact[j]);
            }
        }
        pt[31] = 1;

        Span<byte> fullKey = stackalloc byte[32];
        Span<byte> firstBlockInput = stackalloc byte[16];
        for (int i = 0; i < 16; i++)
        {
            firstBlockInput[i] = (byte)(pt[i] ^ iv[i]);
        }
        AesV3Block(firstBlockInput, fullKey.Slice(0, 16), expanded, box, con, order);

        Span<byte> secondBlockInput = stackalloc byte[16];
        for (int i = 0; i < 16; i++)
        {
            secondBlockInput[i] = (byte)(pt[16 + i] ^ fullKey[i]);
        }
        AesV3Block(secondBlockInput, fullKey.Slice(16, 16), expanded, box, con, order);

        byte[] finalBytes = new byte[1 + source.Length];
        finalBytes[0] = fullKey[31];
        source.CopyTo(finalBytes.AsSpan(1));
        Span<byte> result = finalBytes.AsSpan(1);

        for (int i = 0; i < 31; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                int at = i * 8 + j;
                if ((uint)at < (uint)result.Length)
                {
                    byte cur = result[at];
                    result[at] = (byte)(
                        (cur & (~(1 << Wire[j]) & 0xFF)) | (((fullKey[i] >> Compact[j]) & 1) << Wire[j])
                    );
                }
            }
        }

        return finalBytes;
    }

    private static void AesV3Block(
        ReadOnlySpan<byte> value,
        Span<byte> output,
        ReadOnlySpan<byte> expanded,
        ReadOnlySpan<byte> box,
        int[] con,
        int[] order
    )
    {
        Span<byte> rows = stackalloc byte[16];
        value.CopyTo(rows);

        for (int i = 0; i < 4; i++)
        {
            int rowIdx = i * 4;
            int keyIdx = i * 4;
            for (int j = 0; j < 4; j++)
            {
                rows[rowIdx + j] ^= expanded[keyIdx + con[j]];
            }
        }

        Span<byte> before = stackalloc byte[16];

        for (int rnd = 1; rnd <= 2; rnd++)
        {
            for (int i = 0; i < 16; i++)
            {
                rows[i] = box[rows[i]];
            }

            rows.CopyTo(before);
            for (int i = 0; i < 4; i++)
            {
                int ci = con[i];
                for (int j = 0; j < 4; j++)
                {
                    rows[i * 4 + j] = before[ci * 4 + j];
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
                    int rowIdx = i * 4;
                    for (int j = 0; j < 4; j++)
                    {
                        rows[rowIdx + j] = before[rowIdx + con[j]];
                    }
                }

                static byte Xt(byte x) =>
                    (byte)(((x << 1) ^ ((x & 128) != 0 ? 0x1B : 0)) & 255);

                for (int i = 0; i < 4; i++)
                {
                    byte t = (byte)(rows[i] ^ rows[4 + i] ^ rows[8 + i] ^ rows[12 + i]);
                    byte u = rows[i];
                    rows[i] ^= (byte)(t ^ Xt((byte)(rows[i] ^ rows[4 + i])));
                    rows[4 + i] ^= (byte)(t ^ Xt((byte)(rows[4 + i] ^ rows[8 + i])));
                    rows[8 + i] ^= (byte)(t ^ Xt((byte)(rows[8 + i] ^ rows[12 + i])));
                    rows[12 + i] ^= (byte)(t ^ Xt((byte)(rows[12 + i] ^ u)));
                }
            }

            int off = rnd * 4;
            for (int i = 0; i < 4; i++)
            {
                int rowIdx = i * 4;
                int keyIdx = (off + i) * 4;
                for (int j = 0; j < 4; j++)
                {
                    rows[rowIdx + j] ^= expanded[keyIdx + con[j]];
                }
            }
        }

        for (int i = 0; i < 4; i++)
        {
            int rowIdx = i * 4;
            int keyIdx = (4 + i) * 4;
            for (int j = 0; j < 4; j++)
            {
                rows[rowIdx + j] ^= expanded[keyIdx + j];
            }
        }

        rows.CopyTo(output);
    }

    private readonly record struct ProtoVal(int Type, long IntVal, byte[]? ByteVal)
    {
        public static ProtoVal Sint(long val) => new(1, val, null);

        public static ProtoVal Str(string s) => new(2, 0, Encoding.UTF8.GetBytes(s));

        public static ProtoVal Bytes(byte[] b) => new(2, 0, b);

        public static ProtoVal Flt(float val) => new(3, BitConverter.SingleToInt32Bits(val), null);
    }

    private static byte[] EncodeProto(params (int Tag, ProtoVal Val)[] fields)
    {
        using MemoryStream ms = new();

        void WriteVarint(ulong v)
        {
            while (v >= 0x80)
            {
                ms.WriteByte((byte)((v & 0x7F) | 0x80));
                v >>= 7;
            }
            ms.WriteByte((byte)(v & 0x7F));
        }

        Span<byte> floatBytes = stackalloc byte[4];
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
            else if (val.Type == 3) // 32-bit fixed (wire type 5)
            {
                WriteVarint((ulong)((tag << 3) | 5));
                BinaryPrimitives.WriteInt32LittleEndian(floatBytes, (int)val.IntVal);
                ms.Write(floatBytes);
            }
        }

        return ms.ToArray();
    }

    private static string? ExtractParam(string query, string key)
    {
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        int start = 0;
        while (start < query.Length)
        {
            int eq = query.IndexOf('=', start);
            if (eq < 0)
            {
                break;
            }

            int next = query.IndexOf('&', eq);
            if (next < 0)
            {
                next = query.Length;
            }

            if (eq - start == key.Length && query.AsSpan(start, key.Length).SequenceEqual(key))
            {
                return Uri.UnescapeDataString(query.AsSpan(eq + 1, next - eq - 1));
            }

            start = next + 1;
        }

        return null;
    }
}
