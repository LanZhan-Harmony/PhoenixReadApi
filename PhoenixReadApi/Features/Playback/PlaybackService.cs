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

    internal Task<PlaybackMedia> ResolveAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken
    ) =>
        ResolveCoreAsync(NormalizeSeriesId(seriesId), NormalizeVideoId(videoId), cancellationToken);

    internal Task<PlaybackMedia> ResolveAsync(
        Chapter chapter,
        string seriesId,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(chapter);
        return ResolveAsync(seriesId, chapter.VideoId, cancellationToken);
    }

    private async Task<PlaybackMedia> ResolveCoreAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken
    )
    {
        Exception? appError;
        try
        {
            return await ResolveAppMediaAsync(videoId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            appError = exception;
        }

        Exception? webError = null;
        if (_options.EnableWebFallback)
        {
            try
            {
                return await ResolveWebMediaAsync(seriesId, videoId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (IsRecoverable(exception))
            {
                webError = exception;
            }
        }

        throw new PhoenixReadApiFallbackException(
            "无法获取该分集的播放信息。",
            "media",
            webError ?? appError
        );
    }

    private async Task<PlaybackMedia> ResolveAppMediaAsync(
        string videoId,
        CancellationToken cancellationToken
    )
    {
        AppPlaybackPayload payload = new()
        {
            VideoIds = new Dictionary<string, IReadOnlyList<string>> { ["1004"] = [videoId] },
        };
        AppResponse<Dictionary<string, AppPlaybackItem>> result = await _appTransport
            .PostAsync<AppPlaybackPayload, Dictionary<string, AppPlaybackItem>>(
                "/novel/player/multi_video_model/preload/v1",
                payload,
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);
        if (
            result.Data is null
            || !result.Data.TryGetValue(videoId, out AppPlaybackItem? item)
            || item.VideoModel.Length == 0
        )
        {
            throw new PhoenixReadApiResponseException("未返回播放模型。", "media");
        }

        AppPlaybackModel model =
            JsonAotSerializer.Deserialize<AppPlaybackModel>(item.VideoModel)
            ?? throw new PhoenixReadApiDeserializationException(
                "播放模型格式异常。",
                "media"
            );
        if (model.Variants.Count == 0)
        {
            throw new PhoenixReadApiResponseException("未返回可用媒体。", "media");
        }

        return SelectAppMedia(model);
    }

    private static PlaybackMedia SelectAppMedia(AppPlaybackModel model)
    {
        var durationSeconds = model.VideoDuration > 0 ? model.VideoDuration : model.Duration;
        List<ScoredMedia> choices = [];
        Exception? keyError = null;
        foreach (AppMediaVariantDto variant in model.Variants)
        {
            var codec = variant.Metadata.CodecType.ToLowerInvariant();
            if (
                codec == "bytevc2"
                || (
                    variant.GearDescriptionKey.Contains(
                        "bytevc2",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            {
                continue;
            }

            List<string> addresses = MediaAddresses(variant);
            if (addresses.Count == 0)
            {
                continue;
            }

            var key = string.Empty;
            AppEncryptionDto encryption = variant.Encryption;
            var spade = encryption.SpadeA;
            if (spade.Length > 0 || encryption.Encrypt || encryption.Method == "cenc-aes-ctr")
            {
                try
                {
                    key = Convert.ToHexString(ContentKeyDecoder.Decode(spade)).ToLowerInvariant();
                }
                catch (Exception exception) when (exception is PhoenixReadApiException)
                {
                    keyError = exception;
                    continue;
                }
            }

            var height = variant.Metadata.Height;
            var definition = JsonValueReader.Quality(variant.Metadata.Definition);
            if (definition > 0)
            {
                height = definition;
            }
            else
            {
                var width = variant.Metadata.Width;
                if (width > 0 && (height == 0 || width < height))
                {
                    height = width;
                }
            }

            var qualityScore = height * 10 + (codec is "h264" or "avc1" ? 1 : 0);
            foreach (var address in addresses)
            {
                choices.Add(
                    new ScoredMedia(
                        new PlaybackMedia
                        {
                            Url = address,
                            Referer = "https://novel.snssdk.com/",
                            Duration = TimeSpan.FromSeconds(Math.Max(0, durationSeconds)),
                            Quality = height,
                            DecryptionKey = key,
                        },
                        qualityScore
                    )
                );
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
        PlaybackMedia selected = choices[0].Media;
        return new PlaybackMedia
        {
            Url = selected.Url,
            Referer = selected.Referer,
            Duration = selected.Duration,
            Quality = selected.Quality,
            DecryptionKey = selected.DecryptionKey,
            Variants =
            [
                .. choices.Select(static choice => new MediaVariant
                {
                    Url = choice.Media.Url,
                    Quality = choice.Media.Quality,
                    DecryptionKey = choice.Media.DecryptionKey,
                }),
            ],
        };
    }

    private async Task<PlaybackMedia> ResolveWebMediaAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken
    )
    {
        Uri pageUri = new(
            _options.WebBaseUri,
            $"player/{Uri.EscapeDataString(seriesId)}/{Uri.EscapeDataString(videoId)}"
        );
        var html = await GetTextAsync(pageUri, _options.WebBaseUri, "media", cancellationToken)
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
            || page.SeriesId != seriesId
            || !page.PublicVideoSeo.Available
            || page.PublicVideoSeo.SeriesId != seriesId
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
        var mediaUrl = FindPublicVideoUrl(html, pageUri, videoId);
        if (mediaUrl.Length == 0)
        {
            throw new PhoenixReadApiResponseException("该集未提供公开播放地址。", "media");
        }

        var duration = page.PublicVideoSeo.DurationSeconds;
        MediaVariant[] variants =
        [
            new() { Url = mediaUrl, Quality = JsonValueReader.Quality(mediaUrl) },
        ];
        return new PlaybackMedia
        {
            Url = mediaUrl,
            Referer = new Uri(_options.WebBaseUri, "/").ToString(),
            Duration = TimeSpan.FromSeconds(Math.Max(0, duration)),
            Variants = variants,
        };
    }

    private static string FindPublicVideoUrl(string html, Uri pageUri, string videoId)
    {
        foreach (Match match in JsonLdScriptRegex().Matches(html))
        {
            var rawJson = WebUtility.HtmlDecode(match.Groups["json"].Value).Trim();
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

                var normalized = UrlNormalizer.NormalizeMediaUrl(contentUrl.GetString(), pageUri);
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

    private static List<string> MediaAddresses(AppMediaVariantDto info)
    {
        List<string> addresses = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (
            var raw in (info.MainUrls ?? [])
                .Concat(info.BackupUrls ?? [])
                .Concat(info.BackupUrls1 ?? [])
                .Concat(info.BackupUrls2 ?? [])
                .Concat(info.BackupUrlCollection ?? [])
                .Concat(info.UrlList ?? [])
        )
        {
            Add(raw);
        }
        return addresses;

        void Add(string value)
        {
            var text = value.Trim();
            if (!UrlNormalizer.IsHttpUrl(text))
            {
                try
                {
                    text = Encoding.UTF8.GetString(Base64Decoder.Decode(text)).Trim();
                }
                catch (FormatException)
                {
                    return;
                }
            }

            if (UrlNormalizer.IsHttpUrl(text) && seen.Add(text))
            {
                addresses.Add(text);
            }
        }
    }

    private readonly record struct ScoredMedia(PlaybackMedia Media, int Score);

    [GeneratedRegex(
        "(?is)<script\\b(?=[^>]*\\btype\\s*=\\s*[\\\"']application/ld\\+json[\\\"'])[^>]*>(?<json>.*?)</script\\s*>"
    )]
    private static partial Regex JsonLdScriptRegex();
}
