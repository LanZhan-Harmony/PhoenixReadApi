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
    private const string VideoTabHost = "https://reading.snssdk.com";
    private const string SingleVideoModelPath = "/novel/player/video_model/v1/";
    private const string PlayerPath = "/novel/player/multi_video_model/player/v1";
    private const string PreloadPath = "/novel/player/multi_video_model/preload/v1";
    private const string FlatPath = "/novel/player/multi_video_model/v1/";
    private const string VideoTabPath = "/reading/bookapi/video_tab/multi_video_model/v1/";

    /// <summary>
    /// 进程内记忆最近一次成功的 V5 路径。服务端反向切换时可动态尝试备用路径并在稳态下仅发 1 次请求。
    /// </summary>
    private static volatile string? s_lastGoodPath;

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

    /// <summary>直接以 content_type=1001 解析独立短视频（红果圈、合集子视频、视频搜索）的播放模型数据。</summary>
    internal async Task<AppResponse<AppPlaybackData>> GetShortVideoPlaybackDataAsync(
        string videoId,
        CancellationToken cancellationToken
    )
    {
        await _deviceManager
            .EnsureRegisteredAsync(forceNew: false, cancellationToken)
            .ConfigureAwait(false);

        await _http.EnsureClockSyncedAsync(cancellationToken).ConfigureAwait(false);

        string deviceId = _deviceManager.DeviceId;
        string installId = _deviceManager.InstallId;
        string cdid = string.IsNullOrWhiteSpace(_deviceManager.Cdid)
            ? Guid.NewGuid().ToString()
            : _deviceManager.Cdid;

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + _http.ServerClockOffsetMs;
        uint baseSec = (uint)(nowMs / 1000);

        AppResponse<AppPlaybackData>? result = await RequestSingleVideoModelAsync(
                videoId,
                deviceId,
                installId,
                cdid,
                baseSec,
                nowMs,
                cancellationToken,
                contentType: 1001
            )
            .ConfigureAwait(false);

        if (HasPlaybackData(result, videoId))
        {
            return result!;
        }

        throw new PhoenixReadApiResponseException(
            $"[V5 {SingleVideoModelPath} content_type=1001] 响应未包含有效短视频媒体数据 (Code={result?.Code})",
            SingleVideoModelPath
        );
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

        // 按最近成功路径优先排序候选 V5 路径（优先尝试单集 video_model 端点）
        string goodPath = s_lastGoodPath ?? SingleVideoModelPath;
        string[] orderedPaths = goodPath switch
        {
            PlayerPath => [PlayerPath, SingleVideoModelPath, PreloadPath, FlatPath],
            PreloadPath => [PreloadPath, SingleVideoModelPath, PlayerPath, FlatPath],
            FlatPath => [FlatPath, SingleVideoModelPath, PlayerPath, PreloadPath],
            _ => [SingleVideoModelPath, PlayerPath, PreloadPath, FlatPath],
        };

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
                AppResponse<AppPlaybackData>? v5Result = path == SingleVideoModelPath
                    ? await RequestSingleVideoModelAsync(
                            videoId,
                            deviceId,
                            installId,
                            cdid,
                            baseSec,
                            nowMs,
                            cancellationToken
                        )
                        .ConfigureAwait(false)
                    : await RequestV5Async(
                            path,
                            videoId,
                            deviceId,
                            installId,
                            cdid,
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

        // 1b. 若标准剧集路由未返回数据，尝试 content_type=1001 独立短视频/合集子视频单集接口
        try
        {
            AppResponse<AppPlaybackData>? ugcSingleResult = await RequestSingleVideoModelAsync(
                    videoId,
                    deviceId,
                    installId,
                    cdid,
                    baseSec,
                    nowMs,
                    cancellationToken,
                    contentType: 1001
                )
                .ConfigureAwait(false);

            if (HasPlaybackData(ugcSingleResult, videoId))
            {
                return ugcSingleResult!;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            lastV5Error ??= ex;
        }

        // 2. 若所有 V5 路径均返回空响应或失败，尝试 video_tab GET 回退通道
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
        uint baseSec,
        long nowMs,
        CancellationToken cancellationToken,
        string? groupKeyOverride = null
    )
    {
        // 按照官方最新抓包严格匹配 scene 与分组键：
        // /player/v1 -> dr_scene="player", key="1004"
        // /preload/v1 -> dr_scene="preload", key="1004"
        // 短视频/合集子视频 -> /novel/player/multi_video_model/v1, key="1001"
        string scene = path == PreloadPath ? "preload" : "player";
        string groupKey = groupKeyOverride
            ?? (path == PreloadPath ? "1004" : (path == PlayerPath ? "1004" : "1"));
        string? egdi = _deviceManager.KlinkEgdi;
        AppPlaybackPayload payload = new()
        {
            DrScene = groupKeyOverride is null ? scene : null,
            BizParam = new AppPlaybackBizParam
            {
                DetailPageVersion = 0,
                DeviceLevel = 3,
                DisableDiggStat = false,
                DisableVideoRelateBook = false,
                NeedAllVideoDefinition = true,
                NeedMp4Align = false,
                UseOsPlayer = false,
                UseServerDns = false,
                VideoPlatform = 1024,
            },
            MixedVideoIdMap = new Dictionary<string, List<string>> { [groupKey] = [videoId] },
            VideoId = groupKeyOverride is null ? videoId : null,
            ContentType = groupKeyOverride is null ? 1 : 0,
        };

        byte[] jsonBytes = JsonAotSerializer.SerializeToUtf8Bytes(payload);
        byte[] gzipBody = PhoenixReadPlaybackSigner.CompressJavaGzip(jsonBytes);
        byte[] bodyMd5 = MD5.HashData(gzipBody);
        string stub = Convert.ToHexString(bodyMd5);

        (string prefix, string suffix) = PhoenixReadPlaybackSigner.BuildPlaybackV5UrlParts(
            $"{BaseHost}{path}",
            deviceId,
            installId,
            cdid,
            egdi
        );
        long ticket = PhoenixReadPlaybackSigner.FindTicketMsForBranch12(
            prefix,
            suffix,
            bodyMd5,
            baseSec,
            nowMs
        );
        string fullUrl = $"{prefix}{ticket}{suffix}";

        (
            string khronos,
            string gorgon,
            string helios,
            string medusa,
            string argus,
            string ladon,
            _
        ) = PhoenixReadPlaybackSigner.SignV5(
            fullUrl,
            gzipBody,
            baseSec,
            deviceId,
            installId,
            precomputedBodyMd5: bodyMd5
        );

        using HttpRequestMessage request = new(HttpMethod.Post, fullUrl);
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            PhoenixReadDeviceProfile.PlaybackUserAgent
        );
        request.Headers.TryAddWithoutValidation(
            "Accept",
            "application/json; charset=utf-8,application/x-protobuf"
        );
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation(
            "x-ss-dp",
            PhoenixReadDeviceProfile.AidString
        );
        request.Headers.TryAddWithoutValidation("lc", "101");
        request.Headers.TryAddWithoutValidation("sdk-version", "2");
        request.Headers.TryAddWithoutValidation("passport-sdk-version", "5051452");
        request.Headers.TryAddWithoutValidation("x-vc-bdturing-sdk-version", "4.0.3.cn");
        request.Headers.TryAddWithoutValidation("x-tt-store-region", "cn-ah");
        request.Headers.TryAddWithoutValidation("x-tt-store-region-src", "did");
        request.Headers.TryAddWithoutValidation("x-ss-req-ticket", nowMs.ToString());
        request.Headers.TryAddWithoutValidation(
            "x-reading-request",
            $"{nowMs}-{RandomNumberGenerator.GetInt32(100_000_000, 999_999_999)}"
        );
        request.Headers.TryAddWithoutValidation("x-ss-stub", stub);
        request.Headers.TryAddWithoutValidation("x-khronos", khronos);
        request.Headers.TryAddWithoutValidation("x-gorgon", gorgon);
        request.Headers.TryAddWithoutValidation("x-helios", helios);
        request.Headers.TryAddWithoutValidation("x-medusa", medusa);
        request.Headers.TryAddWithoutValidation("x-argus", argus);
        request.Headers.TryAddWithoutValidation("x-ladon", ladon);

        if (_sessionStore.HasSession)
        {
            _sessionStore.ApplySession(request);
        }

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

    private async Task<AppResponse<AppPlaybackData>?> RequestSingleVideoModelAsync(
        string videoId,
        string deviceId,
        string installId,
        string cdid,
        uint baseSec,
        long nowMs,
        CancellationToken cancellationToken,
        int contentType = 0
    )
    {
        AppSinglePlaybackPayload payload = new()
        {
            VideoId = videoId,
            ContentType = contentType,
            BizParam = new AppSinglePlaybackBizParam
            {
                NeedAllVideoDefinition = true,
                VideoIdType = 0,
                VideoPlatform = 3,
            },
        };

        byte[] jsonBytes = JsonAotSerializer.SerializeToUtf8Bytes(payload);
        byte[] bodyMd5 = MD5.HashData(jsonBytes);
        string stub = Convert.ToHexString(bodyMd5);
        string? egdi = _deviceManager.KlinkEgdi;

        (string prefix, string suffix) = PhoenixReadPlaybackSigner.BuildPlaybackV5UrlParts(
            $"{BaseHost}{SingleVideoModelPath}",
            deviceId,
            installId,
            cdid,
            egdi
        );
        long ticket = PhoenixReadPlaybackSigner.FindTicketMsForBranch12(
            prefix,
            suffix,
            bodyMd5,
            baseSec,
            nowMs
        );
        string fullUrl = $"{prefix}{ticket}{suffix}";

        (
            string khronos,
            string gorgon,
            string helios,
            string medusa,
            string argus,
            string ladon,
            _
        ) = PhoenixReadPlaybackSigner.SignV5(
            fullUrl,
            jsonBytes,
            baseSec,
            deviceId,
            installId,
            precomputedBodyMd5: bodyMd5
        );

        using HttpRequestMessage request = new(HttpMethod.Post, fullUrl);
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            PhoenixReadDeviceProfile.PlaybackUserAgent
        );
        request.Headers.TryAddWithoutValidation(
            "Accept",
            "application/json; charset=utf-8,application/x-protobuf"
        );
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation(
            "x-ss-dp",
            PhoenixReadDeviceProfile.AidString
        );
        request.Headers.TryAddWithoutValidation("lc", "101");
        request.Headers.TryAddWithoutValidation("sdk-version", "2");
        request.Headers.TryAddWithoutValidation("passport-sdk-version", "5051452");
        request.Headers.TryAddWithoutValidation("x-vc-bdturing-sdk-version", "4.0.3.cn");
        request.Headers.TryAddWithoutValidation("x-tt-store-region", "cn-ah");
        request.Headers.TryAddWithoutValidation("x-tt-store-region-src", "did");
        request.Headers.TryAddWithoutValidation("x-ss-req-ticket", nowMs.ToString());
        request.Headers.TryAddWithoutValidation(
            "x-reading-request",
            $"{nowMs}-{RandomNumberGenerator.GetInt32(100_000_000, 999_999_999)}"
        );
        request.Headers.TryAddWithoutValidation("x-ss-stub", stub);
        request.Headers.TryAddWithoutValidation("x-khronos", khronos);
        request.Headers.TryAddWithoutValidation("x-gorgon", gorgon);
        request.Headers.TryAddWithoutValidation("x-helios", helios);
        request.Headers.TryAddWithoutValidation("x-medusa", medusa);
        request.Headers.TryAddWithoutValidation("x-argus", argus);
        request.Headers.TryAddWithoutValidation("x-ladon", ladon);

        if (_sessionStore.HasSession)
        {
            _sessionStore.ApplySession(request);
        }

        request.Content = new ByteArrayContent(jsonBytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8",
        };

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
        string? egdi = _deviceManager.KlinkEgdi;
        string prefix = PhoenixReadPlaybackSigner.BuildVideoTabUrlPrefix(
            $"{VideoTabHost}{VideoTabPath}",
            videoId,
            deviceId,
            installId,
            cdid,
            egdi
        );
        long ticket = PhoenixReadPlaybackSigner.FindTicketMsForBranch12(
            prefix,
            string.Empty,
            zeroMd5,
            baseSec,
            nowMs
        );
        string tabUrl = $"{prefix}{ticket}";

        (
            string khronos,
            string gorgon,
            string helios,
            string medusa,
            string argus,
            string ladon,
            _
        ) = PhoenixReadPlaybackSigner.SignV5(tabUrl, [], baseSec, deviceId);

        using HttpRequestMessage request = new(HttpMethod.Get, tabUrl);
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            PhoenixReadDeviceProfile.PlaybackUserAgent
        );
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("X-XS-From-Web", "0");
        request.Headers.TryAddWithoutValidation("x-khronos", khronos);
        request.Headers.TryAddWithoutValidation("x-gorgon", gorgon);
        request.Headers.TryAddWithoutValidation("x-helios", helios);
        request.Headers.TryAddWithoutValidation("x-medusa", medusa);
        request.Headers.TryAddWithoutValidation("x-argus", argus);
        request.Headers.TryAddWithoutValidation("x-ladon", ladon);

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
