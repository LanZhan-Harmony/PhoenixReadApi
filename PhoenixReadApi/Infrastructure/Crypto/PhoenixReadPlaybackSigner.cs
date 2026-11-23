using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>
/// 红果短剧 V5 播放协议专用签名外观门面类。
/// 实际算法已拆分为：
/// - <see cref="XKhronosSigner"/>
/// - <see cref="XHeliosSigner"/>
/// - <see cref="XMedusaSigner"/>
/// </summary>
internal static class PhoenixReadPlaybackSigner
{
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
            .Append(PhoenixReadDeviceProfile.Aid)
            .Append("&app_name=")
            .Append(PhoenixReadDeviceProfile.AppName)
            .Append("&version_code=")
            .Append(PhoenixReadDeviceProfile.VersionCode)
            .Append("&version_name=")
            .Append(PhoenixReadDeviceProfile.VersionName)
            .Append("&channel=")
            .Append(PhoenixReadDeviceProfile.Channel)
            .Append("&device_platform=")
            .Append(PhoenixReadDeviceProfile.DevicePlatform)
            .Append("&device_type=")
            .Append(PhoenixReadDeviceProfile.DeviceModel)
            .Append("&os_version=")
            .Append(PhoenixReadDeviceProfile.OsVersion)
            .Append("&update_version_code=")
            .Append(PhoenixReadDeviceProfile.VersionCode)
            .Append("&iid=")
            .Append(Uri.EscapeDataString(iid))
            .Append("&cdid=")
            .Append(Uri.EscapeDataString(cdid))
            .Append("&device_id=")
            .Append(Uri.EscapeDataString(deviceId))
            .Append("&_rticket=");
        return sb.ToString();
    }

    /// <summary>
    /// 生成 V5 播放查询 URL，与统一设备 Profile 全面对齐。
    /// </summary>
    internal static string BuildPlaybackV5Url(
        string baseUri,
        string deviceId,
        string installId,
        string cdid,
        long ticketMs,
        string? klinkEgdi = null
    )
    {
        StringBuilder sb = new(512);
        sb.Append(baseUri)
            .Append("?device_platform=")
            .Append(PhoenixReadDeviceProfile.DevicePlatform)
            .Append("&os=android")
            .Append("&ssmix=a")
            .Append("&_rticket=")
            .Append(ticketMs)
            .Append("&cdid=")
            .Append(Uri.EscapeDataString(cdid))
            .Append("&channel=")
            .Append(PhoenixReadDeviceProfile.Channel)
            .Append("&aid=")
            .Append(PhoenixReadDeviceProfile.Aid)
            .Append("&app_name=")
            .Append(PhoenixReadDeviceProfile.AppName)
            .Append("&version_code=")
            .Append(PhoenixReadDeviceProfile.VersionCode)
            .Append("&version_name=")
            .Append(PhoenixReadDeviceProfile.VersionName)
            .Append("&manifest_version_code=")
            .Append(PhoenixReadDeviceProfile.VersionCode)
            .Append("&update_version_code=")
            .Append(PhoenixReadDeviceProfile.VersionCode)
            .Append("&resolution=")
            .Append(Uri.EscapeDataString(PhoenixReadDeviceProfile.Resolution))
            .Append("&dpi=")
            .Append(PhoenixReadDeviceProfile.Dpi)
            .Append("&device_type=")
            .Append(Uri.EscapeDataString(PhoenixReadDeviceProfile.DeviceModel))
            .Append("&device_brand=")
            .Append(Uri.EscapeDataString(PhoenixReadDeviceProfile.DeviceBrand))
            .Append("&language=")
            .Append(PhoenixReadDeviceProfile.Language)
            .Append("&os_api=")
            .Append(PhoenixReadDeviceProfile.OsApi)
            .Append("&os_version=")
            .Append(Uri.EscapeDataString(PhoenixReadDeviceProfile.OsVersion))
            .Append("&ac=")
            .Append(PhoenixReadDeviceProfile.Access)
            .Append("&host_abi=")
            .Append(PhoenixReadDeviceProfile.CpuAbi)
            .Append("&dragon_device_type=phone")
            .Append("&pv_player=")
            .Append(PhoenixReadDeviceProfile.VersionCode)
            .Append("&compliance_status=0")
            .Append("&need_personal_recommend=1")
            .Append("&player_so_load=1")
            .Append("&is_android_pad_screen=0")
            .Append("&rom_version=")
            .Append(Uri.EscapeDataString(PhoenixReadDeviceProfile.RomVersion))
            .Append("&iid=")
            .Append(Uri.EscapeDataString(installId))
            .Append("&device_id=")
            .Append(Uri.EscapeDataString(deviceId));
        if (!string.IsNullOrWhiteSpace(klinkEgdi))
        {
            sb.Append("&klink_egdi=").Append(Uri.EscapeDataString(klinkEgdi));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 生成 VideoTab 回退查询 URL，与统一设备 Profile 全面对齐。
    /// </summary>
    internal static string BuildVideoTabUrl(
        string baseUri,
        string episodeId,
        string deviceId,
        string installId,
        string cdid,
        long ticketMs,
        string? klinkEgdi = null
    )
    {
        StringBuilder sb = new(512);
        sb.Append(baseUri)
            .Append("?device_platform=")
            .Append(PhoenixReadDeviceProfile.DevicePlatform)
            .Append("&os=android")
            .Append("&ssmix=a")
            .Append("&cdid=")
            .Append(Uri.EscapeDataString(cdid))
            .Append("&channel=")
            .Append(PhoenixReadDeviceProfile.Channel)
            .Append("&aid=")
            .Append(PhoenixReadDeviceProfile.Aid)
            .Append("&app_name=")
            .Append(PhoenixReadDeviceProfile.AppName)
            .Append("&version_code=")
            .Append(PhoenixReadDeviceProfile.VersionCode)
            .Append("&version_name=")
            .Append(PhoenixReadDeviceProfile.VersionName)
            .Append("&manifest_version_code=")
            .Append(PhoenixReadDeviceProfile.VersionCode)
            .Append("&update_version_code=")
            .Append(PhoenixReadDeviceProfile.VersionCode)
            .Append("&resolution=")
            .Append(Uri.EscapeDataString(PhoenixReadDeviceProfile.Resolution))
            .Append("&dpi=")
            .Append(PhoenixReadDeviceProfile.Dpi)
            .Append("&device_type=")
            .Append(Uri.EscapeDataString(PhoenixReadDeviceProfile.DeviceModel))
            .Append("&device_brand=")
            .Append(Uri.EscapeDataString(PhoenixReadDeviceProfile.DeviceBrand))
            .Append("&language=")
            .Append(PhoenixReadDeviceProfile.Language)
            .Append("&os_api=")
            .Append(PhoenixReadDeviceProfile.OsApi)
            .Append("&os_version=")
            .Append(Uri.EscapeDataString(PhoenixReadDeviceProfile.OsVersion))
            .Append("&ac=")
            .Append(PhoenixReadDeviceProfile.Access)
            .Append("&host_abi=")
            .Append(PhoenixReadDeviceProfile.CpuAbi)
            .Append("&dragon_device_type=phone")
            .Append("&pv_player=")
            .Append(PhoenixReadDeviceProfile.VersionCode)
            .Append("&compliance_status=0")
            .Append("&need_personal_recommend=1")
            .Append("&player_so_load=1")
            .Append("&is_android_pad_screen=0")
            .Append("&rom_version=")
            .Append(Uri.EscapeDataString(PhoenixReadDeviceProfile.RomVersion))
            .Append("&iid=")
            .Append(Uri.EscapeDataString(installId))
            .Append("&device_id=")
            .Append(Uri.EscapeDataString(deviceId));
        if (!string.IsNullOrWhiteSpace(klinkEgdi))
        {
            sb.Append("&klink_egdi=").Append(Uri.EscapeDataString(klinkEgdi));
        }
        sb.Append("&video_id=")
            .Append(Uri.EscapeDataString(episodeId))
            .Append("&video_id_type=0")
            .Append("&device_level=1")
            .Append("&need_all_video_definition=1")
            .Append("&need_mp4_align=0")
            .Append("&use_os_player=0")
            .Append("&video_platform=1024")
            .Append("&source=0")
            .Append("&from_video_id=")
            .Append("&_rticket=")
            .Append(ticketMs);
        return sb.ToString();
    }

    /// <summary>
    /// 通用分支 12 探测器：在内存中为指定 URL 工厂寻找满足 (iv &amp; 15 == 12) 的 _rticket。
    /// 适用于 V5 POST 与 VideoTab GET 两个通道。
    /// </summary>
    internal static long FindTicketMsForBranch12(
        Func<long, string> urlFactory,
        ReadOnlySpan<byte> bodyMd5,
        uint timestamp,
        long startTicketMs
    )
    {
        Span<byte> tsLe = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(tsLe, timestamp);
        Span<byte> querySm3 = stackalloc byte[32];

        for (int offset = 0; offset < 1024; offset++)
        {
            long candidateTicket = startTicketMs + offset;
            string fullUrl = urlFactory(candidateTicket);
            int qIdx = fullUrl.IndexOf('?');
            string query = qIdx >= 0 ? fullUrl[(qIdx + 1)..] : fullUrl;
            byte[] queryBytes = Encoding.UTF8.GetBytes(query);
            PhoenixReadRequestSigner.Sm3(queryBytes, querySm3);

            uint iv = GetIV(0x20230928u, querySm3);
            iv = GetIV(iv, bodyMd5);
            iv = GetIV(iv, tsLe);
            if ((iv & 15) == 12)
            {
                return candidateTicket;
            }
        }
        return startTicketMs;
    }

    /// <summary>
    /// 在内存中寻找满足 V5 算法分支 12 (iv &amp; 15 == 12) 的最小 _rticket。
    /// </summary>
    internal static long FindTicketMsForBranch12Full(
        string baseUri,
        string deviceId,
        string installId,
        string cdid,
        ReadOnlySpan<byte> bodyMd5,
        uint timestamp,
        long startTicketMs
    ) =>
        FindTicketMsForBranch12(
            ticket => BuildPlaybackV5Url(baseUri, deviceId, installId, cdid, ticket),
            bodyMd5,
            timestamp,
            startTicketMs
        );

    internal static string BuildPlaybackUrl(
        string baseUri,
        string deviceId,
        string iid,
        string cdid,
        long ticketMs
    ) => $"{BuildPlaybackUrlPrefix(baseUri, deviceId, iid, cdid)}{ticketMs}";

    internal static uint GetIV(uint iv, ReadOnlySpan<byte> data)
    {
        uint value = iv;
        for (int i = 0; i < data.Length; i++)
        {
            if ((i & 1) == 0)
            {
                value = (value >> 4) ^ value ^ (value << 6) ^ data[i];
            }
            else
            {
                uint inner = (value >> 7) ^ value ^ (data[i] | (value << 12));
                value = ~inner;
            }
        }
        return value;
    }

    /// <summary>
    /// 在内存中寻找满足 V5 算法分支 12 (iv &amp; 15 == 12) 的最小 _rticket。
    /// 官方服务端严格校验分支 ID；命中分支 12 仅需单次 HTTP 请求即可成功，无需在网络上重试。
    /// </summary>
    internal static long FindTicketMsForBranch12(
        string urlPrefix,
        ReadOnlySpan<byte> bodyMd5,
        uint timestamp,
        long startTicketMs
    )
    {
        Span<byte> tsLe = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(tsLe, timestamp);

        int qIdx = urlPrefix.IndexOf('?');
        string prefixQuery = qIdx >= 0 ? urlPrefix[(qIdx + 1)..] : urlPrefix;

        Span<byte> querySm3 = stackalloc byte[32];
        for (int offset = 0; offset < 1024; offset++)
        {
            long candidateTicket = startTicketMs + offset;
            string query = $"{prefixQuery}{candidateTicket}";
            int byteCount = Encoding.UTF8.GetByteCount(query);
            byte[] queryBytes = Encoding.UTF8.GetBytes(query);
            PhoenixReadRequestSigner.Sm3(queryBytes, querySm3);

            uint iv = GetIV(0x20230928u, querySm3);
            iv = GetIV(iv, bodyMd5);
            iv = GetIV(iv, tsLe);
            if ((iv & 15) == 12)
            {
                return candidateTicket;
            }
        }
        return startTicketMs;
    }

    internal static (
        string Khronos,
        string Gorgon,
        string Helios,
        string Medusa,
        string Argus,
        string Ladon,
        string Stub
    ) SignV5(
        string fullUrl,
        byte[] gzipBody,
        uint timestamp,
        string deviceId,
        string? osVersion = null,
        string? deviceType = null
    )
    {
        string khronos = XKhronosSigner.Format(timestamp);
        int qIdx = fullUrl.IndexOf('?');
        string query = qIdx >= 0 ? fullUrl[(qIdx + 1)..] : fullUrl;
        uint gorgonRand = (uint)RandomNumberGenerator.GetInt32(0, 65536);
        string gorgon = XGorgonSigner.SignPlayback(query, gzipBody, timestamp, gorgonRand);
        string helios = XHeliosSigner.SignV5(timestamp);
        string medusa = XMedusaSigner.SignV5(
            fullUrl,
            gzipBody,
            timestamp,
            deviceId,
            osVersion,
            deviceType
        );

        // V5 播放短 Argus：4 字节 Little-Endian 时间戳 Base64（如 thcEaw==, xBcEaw== 等）
        Span<byte> argusBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(argusBytes, timestamp);
        string argus = Convert.ToBase64String(argusBytes);

        // V5 播放短 Ladon：4 字节 Big-Endian 时间戳 Base64（对齐 lvguo-tv）
        Span<byte> ladonBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(ladonBytes, timestamp);
        string ladon = Convert.ToBase64String(ladonBytes);

        string stub = Convert.ToHexString(MD5.HashData(gzipBody));
        return (khronos, gorgon, helios, medusa, argus, ladon, stub);
    }

    internal static byte[] AesV3Encrypt(byte[] source, byte[] key, byte[] iv, uint timestamp) =>
        XMedusaSigner.AesV3Encrypt(source, key, iv, timestamp);

    internal static string GenerateHelios(uint timestamp, byte[]? randomBytes = null) =>
        XHeliosSigner.SignV5(timestamp, randomBytes);

    internal static string GenerateMedusa(
        string fullUrl,
        byte[] gzipBody,
        uint timestamp,
        string deviceId
    ) => XMedusaSigner.SignV5(fullUrl, gzipBody, timestamp, deviceId);

    internal static string GenerateMedusaCore(
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
        string? deviceType = null
    ) =>
        XMedusaSigner.GenerateMedusaCore(
            fullUrl,
            gzipBody,
            timestamp,
            deviceId,
            nonceB64,
            startMs,
            msgRandom,
            nonce,
            packRand,
            osVersion,
            deviceType
        );

    internal static byte[] EncodeDeviceInfo(
        string deviceId,
        string nonceB64,
        long startMs,
        string osVersion,
        string deviceType
    ) => XMedusaSigner.EncodeDeviceInfo(deviceId, nonceB64, startMs, osVersion, deviceType);

    internal static byte[] HashF13V5(
        ReadOnlySpan<byte> querySm3,
        ReadOnlySpan<byte> bodyMd5,
        uint timestamp
    ) => XMedusaSigner.HashF13V5(querySm3, bodyMd5, timestamp);
}
