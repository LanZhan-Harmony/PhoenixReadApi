using System.Net.Http.Headers;
using System.Security.Cryptography;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Playback;
using PhoenixReadApi.Internal.Infrastructure.Crypto;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>
/// 播放协议专用传输层。负责 V5 多路径动态轮换 (/player/v1 与 /v1) 及 video_tab GET 回退。
/// </summary>
internal sealed class PhoenixReadPlaybackTransport
{
    private const string BaseHost = "https://api5-normal-sinfonlinea.fqnovel.com";
    private const string PlayerPath = "/novel/player/multi_video_model/player/v1";
    private const string FlatPath = "/novel/player/multi_video_model/v1";
    private const string VideoTabPath = "/reading/bookapi/video_tab/multi_video_model/v1/";

    /// <summary>
    /// 进程内记忆最近一次成功的 V5 路径。服务端反向切换时可动态尝试备用路径并在稳态下仅发 1 次请求。
    /// </summary>
    private volatile static string? s_lastGoodPath;

    private readonly PhoenixReadHttpTransport _http;
    private readonly PhoenixReadApiOptions _options;
    private readonly PhoenixReadDeviceManager _deviceManager;
    private readonly PhoenixReadSessionStore _sessionStore;

    internal PhoenixReadPlaybackTransport(
        PhoenixReadHttpTransport http,
        PhoenixReadApiOptions options,
        PhoenixReadDeviceManager deviceManager,
        PhoenixReadSessionStore sessionStore
    )
    {
        _http = http;
        _options = options;
        _deviceManager = deviceManager;
        _sessionStore = sessionStore;
    }

    /// <summary>获取指定视频分集的播放模型数据。</summary>
    internal async Task<AppResponse<AppPlaybackData>> GetPlaybackDataAsync(
        string videoId,
        CancellationToken cancellationToken
    )
    {
        await _deviceManager
            .EnsureRegisteredAsync(forceNew: false, cancellationToken)
            .ConfigureAwait(false);

        await _http.EnsureClockSyncedAsync(cancellationToken).ConfigureAwait(false);

        // 构造 V5 请求体（不带 dr_scene，兼容平铺 /v1 与 /player/v1）
        AppPlaybackPayload payload = new()
        {
            BizParam = new AppPlaybackBizParam
            {
                DetailPageVersion = 0,
                DeviceLevel = 1,
                DisableDiggStat = false,
                DisableVideoRelateBook = false,
                NeedAllVideoDefinition = true,
                NeedMp4Align = false,
                UseOsPlayer = false,
                UseServerDns = false,
                VideoPlatform = 1024,
            },
            MixedVideoIdMap = new Dictionary<string, List<string>> { ["1004"] = [videoId] },
        };

        byte[] jsonBytes = JsonAotSerializer.SerializeToUtf8Bytes(payload);
        byte[] gzipBody = PhoenixReadPlaybackSigner.CompressJavaGzip(jsonBytes);
        byte[] bodyMd5 = MD5.HashData(gzipBody);
        string stub = Convert.ToHexString(bodyMd5);

        // 按最近成功路径优先排序候选 V5 路径
        string goodPath = s_lastGoodPath ?? PlayerPath;
        string[] orderedPaths =
            goodPath == FlatPath ? [FlatPath, PlayerPath] : [PlayerPath, FlatPath];

        string deviceId = _deviceManager.DeviceId;
        string installId = _deviceManager.InstallId;
        string cdid = string.IsNullOrWhiteSpace(_deviceManager.Cdid)
            ? Guid.NewGuid().ToString()
            : _deviceManager.Cdid;

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _http.ServerClockOffsetMs;
        uint baseSec = (uint)(nowMs / 1000);

        Exception? lastV5Error = null;

        // 1. 优先尝试 V5 路径（按分支 12 内存计算 _rticket，命中即仅需单次 HTTP 请求）
        foreach (string path in orderedPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                AppResponse<AppPlaybackData>? v5Result = await RequestV5Async(
                        path,
                        videoId,
                        deviceId,
                        installId,
                        cdid,
                        gzipBody,
                        bodyMd5,
                        stub,
                        baseSec,
                        nowMs,
                        cancellationToken
                    )
                    .ConfigureAwait(false);

                if (HasPlaybackData(v5Result, videoId))
                {
                    s_lastGoodPath = path;
                    return v5Result!;
                }

                lastV5Error = new PhoenixReadApiResponseException(
                    $"[V5 {path}] 响应未包含有效分集媒体数据 (Code={v5Result?.Code})",
                    path
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastV5Error = ex;
            }
        }

        // 2. 若两条 V5 路径均返回空响应或失败，尝试 video_tab GET 回退通道（openhongguo 验证通过）
        Exception? tabError;
        try
        {
            AppResponse<AppPlaybackData>? tabResult = await RequestVideoTabAsync(
                    videoId,
                    deviceId,
                    installId,
                    cdid,
                    baseSec,
                    nowMs,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (HasPlaybackData(tabResult, videoId))
            {
                return tabResult!;
            }

            tabError = new PhoenixReadApiResponseException(
                $"[video_tab] 响应未包含有效分集媒体数据 (Code={tabResult?.Code})",
                VideoTabPath
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            tabError = ex;
        }

        throw new PhoenixReadApiResponseException(
            $"[All playback routes failed] V5: {lastV5Error?.Message}; VideoTab: {tabError?.Message}",
            PlayerPath
        );
    }

    private async Task<AppResponse<AppPlaybackData>?> RequestV5Async(
        string path,
        string videoId,
        string deviceId,
        string installId,
        string cdid,
        byte[] gzipBody,
        byte[] bodyMd5,
        string stub,
        uint baseSec,
        long nowMs,
        CancellationToken cancellationToken
    )
    {
        long ticket = PhoenixReadPlaybackSigner.FindTicketMsForBranch12(
            t =>
                PhoenixReadPlaybackSigner.BuildPlaybackV5Url(
                    $"{BaseHost}{path}",
                    deviceId,
                    installId,
                    cdid,
                    t
                ),
            bodyMd5,
            baseSec,
            nowMs
        );
        string fullUrl = PhoenixReadPlaybackSigner.BuildPlaybackV5Url(
            $"{BaseHost}{path}",
            deviceId,
            installId,
            cdid,
            ticket
        );

        (string khronos, string helios, string medusa, _) = PhoenixReadPlaybackSigner.SignV5(
            fullUrl,
            gzipBody,
            baseSec,
            deviceId
        );

        using HttpRequestMessage request = new(HttpMethod.Post, fullUrl);
        // V5 播放请求不携带游客 Cookie
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            PhoenixReadDeviceProfile.V5PlaybackUserAgent
        );
        request.Headers.TryAddWithoutValidation(
            "Accept",
            "application/json; charset=utf-8,application/x-protobuf"
        );
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("x-ss-dp", "8662");
        request.Headers.TryAddWithoutValidation("lc", "101");
        request.Headers.TryAddWithoutValidation("x-ss-stub", stub);
        request.Headers.TryAddWithoutValidation("x-khronos", khronos);
        request.Headers.TryAddWithoutValidation("x-helios", helios);
        request.Headers.TryAddWithoutValidation("x-medusa", medusa);

        request.Content = new ByteArrayContent(gzipBody);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8",
        };
        request.Content.Headers.ContentEncoding.Add("gzip");

        (HttpResponseMessage response, byte[] bytes) = await _http
            .SendAndReadAsync(request, cancellationToken)
            .ConfigureAwait(false);

        using (response)
        {
            if (response.IsSuccessStatusCode && bytes.Length > 0)
            {
                return JsonAotSerializer.Deserialize<AppResponse<AppPlaybackData>>(bytes);
            }
            return null;
        }
    }

    private async Task<AppResponse<AppPlaybackData>?> RequestVideoTabAsync(
        string videoId,
        string deviceId,
        string installId,
        string cdid,
        uint baseSec,
        long nowMs,
        CancellationToken cancellationToken
    )
    {
        Span<byte> zeroMd5 = stackalloc byte[16];
        long ticket = PhoenixReadPlaybackSigner.FindTicketMsForBranch12(
            t =>
                PhoenixReadPlaybackSigner.BuildVideoTabUrl(
                    $"{BaseHost}{VideoTabPath}",
                    videoId,
                    deviceId,
                    installId,
                    cdid,
                    t
                ),
            zeroMd5,
            baseSec,
            nowMs
        );
        string tabUrl = PhoenixReadPlaybackSigner.BuildVideoTabUrl(
            $"{BaseHost}{VideoTabPath}",
            videoId,
            deviceId,
            installId,
            cdid,
            ticket
        );

        string khronos = XKhronosSigner.Format(baseSec);
        string helios = XHeliosSigner.SignV5(baseSec);
        string medusa = XMedusaSigner.SignV5(tabUrl, [], baseSec, deviceId);

        using HttpRequestMessage request = new(HttpMethod.Get, tabUrl);
        // VideoTab 请求不携带 Cookie、Content-Type 或 x-ss-stub，只携带 V5 的 Medusa、Helios 和 X-Khronos 头
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            PhoenixReadDeviceProfile.VideoTabUserAgent
        );
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("x-khronos", khronos);
        request.Headers.TryAddWithoutValidation("x-helios", helios);
        request.Headers.TryAddWithoutValidation("x-medusa", medusa);

        (HttpResponseMessage response, byte[] bytes) = await _http
            .SendAndReadAsync(request, cancellationToken)
            .ConfigureAwait(false);

        using (response)
        {
            if (response.IsSuccessStatusCode && bytes.Length > 0)
            {
                return JsonAotSerializer.Deserialize<AppResponse<AppPlaybackData>>(bytes);
            }
            return null;
        }
    }

    private static bool HasPlaybackData(AppResponse<AppPlaybackData>? result, string videoId)
    {
        if (result is null || (!string.IsNullOrWhiteSpace(result.Code) && result.Code != "0"))
        {
            return false;
        }

        AppPlaybackData? data = result.Data;
        if (data is null)
        {
            return false;
        }

        if (
            data.VideoModelData is not null
            && data.VideoModelData.TryGetValue(videoId, out AppMultiPlaybackEntry? entry1)
            && entry1?.VideoModel is not null
        )
        {
            return entry1.VideoModel.Variants.Count > 0
                || (entry1.VideoModel.DynamicVideo?.DynamicVideoList is { Count: > 0 });
        }

        if (
            data.Group1004 is not null
            && data.Group1004.TryGetValue(videoId, out AppMultiPlaybackEntry? entry2)
            && entry2?.VideoModel is not null
        )
        {
            return entry2.VideoModel.Variants.Count > 0
                || (entry2.VideoModel.DynamicVideo?.DynamicVideoList is { Count: > 0 });
        }

        if (data.VideoModel.Variants.Count > 0)
        {
            return true;
        }

        if (data.VideoList.Count > 0)
        {
            return true;
        }

        if (data.AdditionalData is not null && data.AdditionalData.ContainsKey(videoId))
        {
            return true;
        }

        return false;
    }
}
