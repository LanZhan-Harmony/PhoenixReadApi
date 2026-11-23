using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Playback;
using PhoenixReadApi.Internal.Contracts.Web.Playback;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Crypto;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Playback;

/// <summary>解析分集的可播放媒体。</summary>
internal sealed partial class PlaybackService : PhoenixFeatureService
{
    internal PlaybackService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal Task<IReadOnlyList<PlaybackMedia>> ResolveAsync(
        string dramaId,
        string videoId,
        CancellationToken cancellationToken
    ) => ResolveCoreAsync(NormalizeDramaId(dramaId), NormalizeVideoId(videoId), cancellationToken);

    private async Task<IReadOnlyList<PlaybackMedia>> ResolveCoreAsync(
        string dramaId,
        string videoId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await ResolveAppMediaAsync(videoId, cancellationToken).ConfigureAwait(false);
        }
        catch (PhoenixReadApiException ex) when (dramaId.Length > 0)
        {
            try
            {
                return await ResolveWebMediaAsync(dramaId, videoId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception webEx)
            {
                throw new PhoenixReadApiFallbackException(
                    $"App 与 Web 媒体接口均解析失败 (App: {ex.Message}, Web: {webEx.Message})",
                    "media",
                    webEx
                );
            }
        }
    }

    private async Task<IReadOnlyList<PlaybackMedia>> ResolveAppMediaAsync(
        string videoId,
        CancellationToken cancellationToken
    )
    {
        AppResponse<AppPlaybackData> result = await _playbackTransport
            .GetPlaybackDataAsync(videoId, cancellationToken)
            .ConfigureAwait(false);

        AppPlaybackData? data = result.Data;
        AppPlaybackModel? model = null;

        if (
            data?.Group1004 is not null
            && data.Group1004.TryGetValue(videoId, out AppMultiPlaybackEntry? entry)
            && entry?.VideoModel is not null
        )
        {
            model = entry.VideoModel;
        }
        else if (
            data?.VideoModelData is not null
            && data.VideoModelData.TryGetValue(videoId, out AppMultiPlaybackEntry? entry2)
            && entry2?.VideoModel is not null
        )
        {
            model = entry2.VideoModel;
        }
        else if (data?.VideoModel is not null && data.VideoModel.Variants.Count > 0)
        {
            model = data.VideoModel;
        }
        else if (
            data?.AdditionalData is not null
            && data.AdditionalData.TryGetValue(videoId, out JsonElement element)
        )
        {
            try
            {
                AppMultiPlaybackEntry? customEntry =
                    JsonAotSerializer.Deserialize<AppMultiPlaybackEntry>(
                        Encoding.UTF8.GetBytes(element.GetRawText())
                    );
                model = customEntry?.VideoModel;
            }
            catch
            {
                // ignore
            }
        }

        model ??= data?.VideoModel ?? new();

        IReadOnlyList<AppMediaVariantDto> variants =
            model.Variants.Count > 0 ? model.Variants
            : model.DynamicVideo?.DynamicVideoList is { Count: > 0 } dynamicList ? dynamicList
            : data?.VideoList ?? [];

        if (variants.Count == 0)
        {
            throw new PhoenixReadApiResponseException(
                "App 播放信息未包含有效的分档列表。",
                "media"
            );
        }

        double duration =
            model.VideoDuration > 0 ? model.VideoDuration
            : model.Duration > 0 ? model.Duration
            : data?.VideoDuration > 0 ? data.VideoDuration
            : data?.Duration ?? 0;

        string audioUrl = string.Empty;
        if (model.DynamicVideo?.DynamicAudioList is { Count: > 0 } audioList)
        {
            audioUrl = PhoenixReadPlaybackUrlDecoder.Decode(audioList[0].MainUrl, model.KeySeed);
        }

        long expiresAt =
            model.UrlExpire > 0 ? model.UrlExpire
            : model.ExpireTime > 0 ? model.ExpireTime
            : data?.ExpireTime ?? 0;

        AppPlaybackModel effectiveModel = new()
        {
            VideoDuration = duration,
            Duration = duration,
            UrlExpire = expiresAt,
            ExpireTime = expiresAt,
            KeySeed = model.KeySeed,
            Variants = variants,
            DynamicVideo = model.DynamicVideo ?? new(),
        };

        return SelectAppMedia(effectiveModel, audioUrl, expiresAt);
    }

    private static IReadOnlyList<PlaybackMedia> SelectAppMedia(
        AppPlaybackModel model,
        string audioUrl,
        long expiresAt
    )
    {
        double durationSeconds = model.VideoDuration > 0 ? model.VideoDuration : model.Duration;
        List<ScoredMedia> choices = [];
        Exception? keyError = null;
        foreach (AppMediaVariantDto variant in model.Variants)
        {
            string codecRaw = variant.EffectiveCodecType.ToLowerInvariant();
            if (
                codecRaw == "bytevc2"
                || variant.GearDescriptionKey.Contains(
                    "bytevc2",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                continue;
            }

            List<string> addresses = MediaAddresses(variant, model.KeySeed);
            if (addresses.Count == 0)
            {
                continue;
            }

            string key = string.Empty;
            string spade = variant.EffectiveSpadeA;
            if (
                spade.Length > 0
                || variant.EffectiveEncrypt
                || variant.EffectiveEncryptionMethod == "cenc-aes-ctr"
            )
            {
                try
                {
                    key = Convert.ToHexStringLower(ContentKeyDecoder.Decode(spade));
                }
                catch (Exception exception) when (exception is PhoenixReadApiException)
                {
                    keyError = exception;
                    continue;
                }
            }

            int height = variant.EffectiveHeight;
            int defScore = ValueNormalizer.Quality(variant.EffectiveDefinition);
            if (defScore > 0)
            {
                height = defScore;
            }
            else
            {
                int width = variant.EffectiveWidth;
                if (width > 0 && (height == 0 || width < height))
                {
                    height = width;
                }
            }

            MediaDefinition definition = ValueNormalizer.ParseDefinition(
                height,
                variant.EffectiveDefinition
            );
            MediaCodec codec = ValueNormalizer.ParseCodec(codecRaw);
            int qualityScore = height * 10 + (codec == MediaCodec.H264 ? 1 : 0);

            string backupUrl = PhoenixReadPlaybackUrlDecoder.Decode(
                variant.EffectiveBackupUrl,
                model.KeySeed
            );
            foreach (string address in addresses)
            {
                PlaybackMedia media = new()
                {
                    Url = address,
                    BackupUrl = backupUrl,
                    Referer = "https://novel.snssdk.com/",
                    Duration = TimeSpan.FromSeconds(Math.Max(0, durationSeconds)),
                    Definition = definition,
                    QualityName = variant.EffectiveQualityName,
                    Codec = codec,
                    Width = variant.EffectiveWidth,
                    Height = height,
                    Bitrate = variant.EffectiveBitrate,
                    Size = variant.EffectiveSize,
                    Quality = height,
                    DecryptionKey = key,
                    AudioUrl = audioUrl,
                    ExpiresAtUnixSeconds = expiresAt,
                    IsHls = variant.EffectiveIsHls,
                    IsDash = variant.EffectiveIsDash,
                };

                choices.Add(new ScoredMedia(media, qualityScore));
            }
        }

        if (choices.Count == 0)
        {
            if (keyError is not null)
            {
                throw new PhoenixReadApiMediaException("App 媒体密钥不可用。", "media", keyError);
            }

            throw new PhoenixReadApiMediaException(
                "App 未返回兼容的媒体，已跳过不支持的编码。",
                "media"
            );
        }

        choices.Sort(static (left, right) => right.Score.CompareTo(left.Score));
        return [.. choices.Select(static choice => choice.Media)];
    }

    private async Task<IReadOnlyList<PlaybackMedia>> ResolveWebMediaAsync(
        string dramaId,
        string videoId,
        CancellationToken cancellationToken
    )
    {
        Uri pageUri = new(
            _options.WebBaseUri,
            $"player/{Uri.EscapeDataString(dramaId)}/{Uri.EscapeDataString(videoId)}"
        );
        string html = await GetTextAsync(pageUri, _options.WebBaseUri, "media", cancellationToken)
            .ConfigureAwait(false);
        using JsonDocument? document = RouterDataExtractor.Parse(html);
        WebPlayerPageDto? page = RouterDataLocator.Deserialize<WebPlayerPageDto>(
            document,
            "player_",
            "player_page"
        );
        if (
            page is null
            || !page.IsSuccess
            || page.VideoId != videoId
            || page.SeriesId != dramaId
            || !page.PublicVideoSeo.Available
            || page.PublicVideoSeo.SeriesId != dramaId
            || page.PublicVideoSeo.ItemId != videoId
        )
        {
            throw new PhoenixReadApiResponseException(
                "网页未返回所请求的剧集，可能仅允许试看。",
                "media"
            );
        }

        // 当前网页播放器把公开 MP4 放在 JSON-LD VideoObject.contentUrl，
        // loaderData.videoList 只是空对象，不能从旧的 video_player_info 字段读取。
        string mediaUrl = FindPublicVideoUrl(html, pageUri, videoId);
        if (mediaUrl.Length == 0)
        {
            throw new PhoenixReadApiResponseException("该集未提供公开播放地址。", "media");
        }

        double duration = page.PublicVideoSeo.DurationSeconds;
        int lastSlash = mediaUrl.LastIndexOf('/');
        string fileName = lastSlash >= 0 ? mediaUrl[(lastSlash + 1)..] : mediaUrl;
        int quality = ValueNormalizer.Quality(fileName);
        if (quality == 0)
        {
            quality = 360;
        }

        PlaybackMedia webMedia = new()
        {
            Url = mediaUrl,
            Referer = new Uri(_options.WebBaseUri, "/").ToString(),
            Duration = TimeSpan.FromSeconds(Math.Max(0, duration)),
            Definition = ValueNormalizer.ParseDefinition(quality, string.Empty),
            Codec = MediaCodec.H264,
            Height = quality,
            Quality = quality,
        };

        return [webMedia];
    }

    private static string FindPublicVideoUrl(string html, Uri pageUri, string videoId)
    {
        foreach (Match match in JsonLdScriptRegex().Matches(html))
        {
            string rawJson = WebUtility.HtmlDecode(match.Groups["json"].Value).Trim();
            if (rawJson.Length == 0)
            {
                continue;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(rawJson);
                JsonElement root = document.RootElement;
                if (
                    !root.TryGetProperty("@type", out JsonElement type)
                    || type.ValueKind != JsonValueKind.String
                    || !string.Equals(type.GetString(), "VideoObject", StringComparison.Ordinal)
                    || !root.TryGetProperty("contentUrl", out JsonElement contentUrl)
                    || contentUrl.ValueKind != JsonValueKind.String
                )
                {
                    continue;
                }

                string normalized = UrlNormalizer.NormalizeMediaUrl(
                    contentUrl.GetString(),
                    pageUri
                );
                if (
                    normalized.Length == 0
                    || !Uri.TryCreate(normalized, UriKind.Absolute, out Uri? mediaUri)
                    || !string.Equals(
                        mediaUri.Host,
                        pageUri.Host,
                        StringComparison.OrdinalIgnoreCase
                    )
                    || !mediaUri.AbsolutePath.StartsWith(
                        "/video/public/",
                        StringComparison.OrdinalIgnoreCase
                    )
                    || !mediaUri.AbsolutePath.Contains(
                        '/' + videoId + '/',
                        StringComparison.Ordinal
                    )
                )
                {
                    continue;
                }

                return normalized;
            }
            catch (JsonException)
            {
                // Other JSON-LD blocks on the page are optional and may be arrays.
            }
        }

        return string.Empty;
    }

    private static List<string> MediaAddresses(AppMediaVariantDto info, string? keySeed)
    {
        List<string> addresses = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (
            string raw in (info.MainUrls ?? [])
                .Concat(info.BackupUrls ?? [])
                .Concat(info.BackupUrls1 ?? [])
                .Concat(info.BackupUrls2 ?? [])
                .Concat(info.BackupUrlCollection ?? [])
                .Concat(info.UrlList ?? [])
        )
        {
            AddAddress(addresses, seen, raw, keySeed);
        }
        return addresses;
    }

    private static void AddAddress(
        List<string> addresses,
        HashSet<string> seen,
        string value,
        string? keySeed
    )
    {
        string text = PhoenixReadPlaybackUrlDecoder.Decode(value, keySeed);
        if (UrlNormalizer.IsHttpUrl(text) && seen.Add(text))
        {
            addresses.Add(text);
        }
    }

    private readonly record struct ScoredMedia(PlaybackMedia Media, int Score);

    [GeneratedRegex(
        "(?is)<script\\b(?=[^>]*\\btype\\s*=\\s*[\\\"']application/ld\\+json[\\\"'])[^>]*>(?<json>.*?)</script\\s*>"
    )]
    private static partial Regex JsonLdScriptRegex();
}
