using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
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

    /// <summary>
    /// 生成 V5 播放查询 URL 的前后缀部分，便于高效拼装与分支探测。
    /// </summary>
    internal static (string Prefix, string Suffix) BuildPlaybackV5UrlParts(
        string baseUri,
        string deviceId,
        string installId,
        string cdid,
        string? klinkEgdi = null
    )
    {
        string prefix = $"{baseUri}?device_platform={PhoenixReadDeviceProfile.DevicePlatform}&os=android&ssmix=a&_rticket=";
        string suffix =
            $"&cdid={Uri.EscapeDataString(cdid)}"
            + $"&channel={PhoenixReadDeviceProfile.Channel}"
            + $"&aid={PhoenixReadDeviceProfile.AidString}"
            + $"&app_name={PhoenixReadDeviceProfile.AppName}"
            + $"&version_code={PhoenixReadDeviceProfile.VersionCode}"
            + $"&version_name={PhoenixReadDeviceProfile.VersionName}"
            + $"&manifest_version_code={PhoenixReadDeviceProfile.VersionCode}"
            + $"&update_version_code={PhoenixReadDeviceProfile.VersionCode}"
            + $"&resolution={Uri.EscapeDataString(PhoenixReadDeviceProfile.Resolution)}"
            + $"&dpi={PhoenixReadDeviceProfile.Dpi}"
            + $"&device_type={Uri.EscapeDataString(PhoenixReadDeviceProfile.DeviceModel)}"
            + $"&device_brand={Uri.EscapeDataString(PhoenixReadDeviceProfile.DeviceBrand)}"
            + $"&language={PhoenixReadDeviceProfile.Language}"
            + $"&os_api={PhoenixReadDeviceProfile.OsApi}"
            + $"&os_version={Uri.EscapeDataString(PhoenixReadDeviceProfile.OsVersion)}"
            + $"&ac={PhoenixReadDeviceProfile.Access}"
            + $"&host_abi={PhoenixReadDeviceProfile.CpuAbi}"
            + "&dragon_device_type=phone"
            + $"&pv_player={PhoenixReadDeviceProfile.VersionCode}"
            + "&compliance_status=0&need_personal_recommend=1&player_so_load=1&is_android_pad_screen=0"
            + $"&rom_version={Uri.EscapeDataString(PhoenixReadDeviceProfile.RomVersion)}"
            + $"&iid={Uri.EscapeDataString(installId)}"
            + $"&device_id={Uri.EscapeDataString(deviceId)}"
            + (string.IsNullOrWhiteSpace(klinkEgdi) ? string.Empty : $"&klink_egdi={Uri.EscapeDataString(klinkEgdi)}");

        return (prefix, suffix);
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
        (string prefix, string suffix) = BuildPlaybackV5UrlParts(baseUri, deviceId, installId, cdid, klinkEgdi);
        return $"{prefix}{ticketMs}{suffix}";
    }

    /// <summary>
    /// 生成 VideoTab 回退查询 URL 前缀，以 &amp;_rticket= 结尾。
    /// </summary>
    internal static string BuildVideoTabUrlPrefix(
        string baseUri,
        string episodeId,
        string deviceId,
        string installId,
        string cdid,
        string? klinkEgdi = null
    )
    {
        return $"{baseUri}?device_platform={PhoenixReadDeviceProfile.DevicePlatform}"
            + "&os=android&ssmix=a"
            + $"&cdid={Uri.EscapeDataString(cdid)}"
            + $"&channel={PhoenixReadDeviceProfile.Channel}"
            + $"&aid={PhoenixReadDeviceProfile.AidString}"
            + $"&app_name={PhoenixReadDeviceProfile.AppName}"
            + $"&version_code={PhoenixReadDeviceProfile.VersionCode}"
            + $"&version_name={PhoenixReadDeviceProfile.VersionName}"
            + $"&manifest_version_code={PhoenixReadDeviceProfile.VersionCode}"
            + $"&update_version_code={PhoenixReadDeviceProfile.VersionCode}"
            + $"&resolution={Uri.EscapeDataString(PhoenixReadDeviceProfile.Resolution)}"
            + $"&dpi={PhoenixReadDeviceProfile.Dpi}"
            + $"&device_type={Uri.EscapeDataString(PhoenixReadDeviceProfile.DeviceModel)}"
            + $"&device_brand={Uri.EscapeDataString(PhoenixReadDeviceProfile.DeviceBrand)}"
            + $"&language={PhoenixReadDeviceProfile.Language}"
            + $"&os_api={PhoenixReadDeviceProfile.OsApi}"
            + $"&os_version={Uri.EscapeDataString(PhoenixReadDeviceProfile.OsVersion)}"
            + $"&ac={PhoenixReadDeviceProfile.Access}"
            + $"&host_abi={PhoenixReadDeviceProfile.CpuAbi}"
            + "&dragon_device_type=phone"
            + $"&pv_player={PhoenixReadDeviceProfile.VersionCode}"
            + "&compliance_status=0&need_personal_recommend=1&player_so_load=1&is_android_pad_screen=0"
            + $"&rom_version={Uri.EscapeDataString(PhoenixReadDeviceProfile.RomVersion)}"
            + $"&iid={Uri.EscapeDataString(installId)}"
            + $"&device_id={Uri.EscapeDataString(deviceId)}"
            + (string.IsNullOrWhiteSpace(klinkEgdi) ? string.Empty : $"&klink_egdi={Uri.EscapeDataString(klinkEgdi)}")
            + $"&video_id={Uri.EscapeDataString(episodeId)}"
            + "&video_id_type=0&device_level=1&need_all_video_definition=1&need_mp4_align=0&use_os_player=0&video_platform=1024&source=0&from_video_id=&_rticket=";
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
    ) => $"{BuildVideoTabUrlPrefix(baseUri, episodeId, deviceId, installId, cdid, klinkEgdi)}{ticketMs}";

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
    /// 通用分支 12 探测器：在内存中为指定 URL 查找满足 (iv &amp; 15 == 12) 的 _rticket。
    /// 预先编码 URL 前缀和后缀，在 1024 轮探测中实现 0 堆内存分配与超高速 SM3 碰撞。
    /// 官方服务端严格校验分支 ID；命中分支 12 仅需单次 HTTP 请求即可成功，无需在网络上重试。
    /// </summary>
    internal static long FindTicketMsForBranch12(
        string urlPrefix,
        string? urlSuffix,
        ReadOnlySpan<byte> bodyMd5,
        uint timestamp,
        long startTicketMs
    )
    {
        Span<byte> tsLe = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(tsLe, timestamp);

        int qIdx = urlPrefix.IndexOf('?');
        string prefixQuery = qIdx >= 0 ? urlPrefix[(qIdx + 1)..] : urlPrefix;
        string suffixQuery = urlSuffix ?? string.Empty;

        int prefixByteCount = Encoding.UTF8.GetByteCount(prefixQuery);
        int suffixByteCount = Encoding.UTF8.GetByteCount(suffixQuery);

        int maxQueryLen = prefixByteCount + 32 + suffixByteCount;
        byte[]? rented = null;
        Span<byte> buffer = maxQueryLen <= 2048
            ? stackalloc byte[maxQueryLen]
            : (rented = ArrayPool<byte>.Shared.Rent(maxQueryLen));

        try
        {
            Encoding.UTF8.GetBytes(prefixQuery, buffer);
            Span<byte> suffixSpan = stackalloc byte[suffixByteCount];
            if (suffixByteCount > 0)
            {
                Encoding.UTF8.GetBytes(suffixQuery, suffixSpan);
            }

            Span<byte> querySm3 = stackalloc byte[32];

            for (int offset = 0; offset < 1024; offset++)
            {
                long candidateTicket = startTicketMs + offset;
                Utf8Formatter.TryFormat(candidateTicket, buffer[prefixByteCount..], out int ticketBytesWritten);

                if (suffixByteCount > 0)
                {
                    suffixSpan.CopyTo(buffer.Slice(prefixByteCount + ticketBytesWritten));
                }

                int totalLen = prefixByteCount + ticketBytesWritten + suffixByteCount;
                PhoenixReadRequestSigner.Sm3(buffer.Slice(0, totalLen), querySm3);

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
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
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
        byte[] body,
        uint timestamp,
        string deviceId,
        string? installId = null,
        string? osVersion = null,
        string? deviceType = null,
        byte[]? precomputedBodyMd5 = null
    )
    {
        string khronos = XKhronosSigner.Format(timestamp);
        int qIdx = fullUrl.IndexOf('?');
        string query = qIdx >= 0 ? fullUrl[(qIdx + 1)..] : fullUrl;
        uint gorgonRand = (uint)RandomNumberGenerator.GetInt32(0, 65536);
        string gorgon = XGorgonSigner.SignPlayback(query, body, timestamp, gorgonRand);
        string helios = XHeliosSigner.SignV5(timestamp);
        string medusa = XMedusaSigner.SignV5(
            fullUrl,
            body,
            timestamp,
            deviceId,
            osVersion,
            deviceType
        );

        string effectiveInstallId = string.IsNullOrWhiteSpace(installId) ? deviceId : installId;
        string aid = PhoenixReadDeviceProfile.AidString;
        byte[] bodyMd5 = precomputedBodyMd5 ?? (body.Length > 0 ? MD5.HashData(body) : []);
        uint nonceArgus = (uint)RandomNumberGenerator.GetInt32(0, int.MaxValue);
        uint nonceLadon = (uint)RandomNumberGenerator.GetInt32(0, int.MaxValue);
        string argus = XArgusSigner.SignPlayback(
            query,
            bodyMd5,
            timestamp,
            nonceArgus,
            aid,
            deviceId,
            effectiveInstallId
        );
        string ladon = XLadonSigner.SignPlayback(timestamp, nonceLadon, aid);

        string stub = Convert.ToHexString(bodyMd5.Length > 0 ? bodyMd5 : MD5.HashData([]));
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
