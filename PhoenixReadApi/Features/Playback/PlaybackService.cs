using System.Text;
using System.Text.Json;
using PhoenixReadApi.Contracts.App.Playback;
using PhoenixReadApi.Contracts.Web.Playback;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Playback;

/// <summary>通过 App、网页和可选备用接口解析媒体。</summary>
internal sealed class PlaybackService : PhoenixFeatureService
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
        return ResolveAsync(seriesId, chapter.VideoUrl, cancellationToken);
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

        if (_options.EnablePlaybackApiFallback)
        {
            try
            {
                return await ResolveBackupMediaAsync(seriesId, videoId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (IsRecoverable(exception))
            {
                var message =
                    $"App 取流失败：{PublicError(appError!)}；网页取流失败：{PublicError(webError!)}；备用取流失败：{PublicError(exception)}";
                throw new PhoenixReadApiFallbackException(message, "media", exception);
            }
        }

        throw new PhoenixReadApiFallbackException(
            $"App 取流失败：{PublicError(appError!)}；网页取流失败：{PublicError(webError!)}",
            "media",
            webError ?? appError
        );
    }

    private async Task<PlaybackMedia> ResolveAppMediaAsync(
        string videoId,
        CancellationToken cancellationToken
    )
    {
        AppResponse<AppPlaybackData> result = await _appTransport
            .PostAsync<AppMediaPayload, AppPlaybackData>(
                "/novel/player/video_model/v1/",
                new AppMediaPayload { VideoId = videoId },
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);
        if (result.Data?.VideoModel is not { } model)
        {
            throw new PhoenixReadApiResponseException("App 播放信息格式异常。", "media");
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
            var codec = variant.Metadata?.CodecType?.ToLowerInvariant() ?? string.Empty;
            if (
                codec == "bytevc2"
                || (
                    variant.GearDescriptionKey?.Contains(
                        "bytevc2",
                        StringComparison.OrdinalIgnoreCase
                    ) ?? false
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
            AppEncryptionDto? encryption = variant.Encryption;
            var spade = encryption?.SpadeA ?? string.Empty;
            if (
                spade.Length > 0
                || encryption?.Encrypt == true
                || encryption?.Method == "cenc-aes-ctr"
            )
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

            var height = variant.Metadata?.Height ?? 0;
            var definition = JsonValueReader.Quality(variant.Metadata?.Definition ?? string.Empty);
            if (definition > 0)
            {
                height = definition;
            }
            else
            {
                var width = variant.Metadata?.Width ?? 0;
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
        using JsonDocument? document = RouterDataExtractor.Parse(
            await GetTextAsync(pageUri, _options.WebBaseUri, "media", cancellationToken)
                .ConfigureAwait(false)
        );
        WebPlayerPageDto? page = RouterDataLocator.Deserialize<WebPlayerPageDto>(
            document,
            "player_",
            "player_page"
        );
        if (page is null || page.VideoId != videoId || page.SeriesId != seriesId)
        {
            throw new PhoenixReadApiResponseException(
                "网页未返回所请求的剧集，可能仅允许试看。",
                "media"
            );
        }

        WebPlayerInfoDto? info = page.PlayerInfo;
        List<string> addresses = info is null ? [] : MediaAddresses(info);
        if (addresses.Count == 0)
        {
            throw new PhoenixReadApiResponseException("该集未提供公开播放地址。", "media");
        }

        var duration = info?.Duration ?? 0;
        MediaVariant[] variants = [.. addresses.Select(url => new MediaVariant { Url = url })];
        return new PlaybackMedia
        {
            Url = addresses[0],
            Referer = new Uri(_options.WebBaseUri, "/").ToString(),
            Duration = TimeSpan.FromSeconds(Math.Max(0, duration)),
            Variants = variants,
        };
    }

    private async Task<PlaybackMedia> ResolveBackupMediaAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken
    )
    {
        PlaybackReference reference = new() { SeriesId = seriesId, VideoId = videoId };
        var json = JsonAotSerializer.SerializeToUtf8Bytes(reference);
        Uri endpoint = new UriBuilder(_options.PlaybackApiUri)
        {
            Query = "id=" + Uri.EscapeDataString(Convert.ToBase64String(json)),
        }.Uri;
        var body = await GetTextAsync(endpoint, _options.WebBaseUri, "playback", cancellationToken)
            .ConfigureAwait(false);
        BackupPlaybackResponseDto response;
        try
        {
            response =
                JsonAotSerializer.Deserialize<BackupPlaybackResponseDto>(
                    PlaybackResponseDecoder.Decode(body)
                ) ?? throw new JsonException("Backup playback response was null.");
        }
        catch (JsonException exception)
        {
            throw new PhoenixReadApiDeserializationException(
                "备用播放接口返回格式异常。",
                "playback",
                exception
            );
        }

        if (
            response.Parse is not (null or "" or "false" or "0")
            || response.Jx is not (null or "" or "false" or "0")
        )
        {
            throw new PhoenixReadApiResponseException(
                "备用播放接口没有返回直接媒体地址。",
                "playback"
            );
        }

        List<ScoredMedia> choices = [];
        Exception? keyError = null;
        foreach (BackupMediaOptionDto option in response.KeyUrls)
        {
            var mediaUrl = option.Source?.Trim() ?? string.Empty;
            var keyId = option.KeyId?.Trim() ?? string.Empty;
            if (!UrlNormalizer.IsHttpUrl(mediaUrl))
            {
                continue;
            }

            byte[] keyIdBytes;
            try
            {
                keyIdBytes = Convert.FromHexString(keyId);
            }
            catch (FormatException)
            {
                continue;
            }
            if (keyIdBytes.Length != 16)
            {
                continue;
            }

            try
            {
                var key = Convert
                    .ToHexString(ContentKeyDecoder.Decode(option.SpadeA ?? string.Empty))
                    .ToLowerInvariant();
                var quality = JsonValueReader.Quality(option.Name ?? string.Empty);
                choices.Add(
                    new ScoredMedia(
                        new PlaybackMedia
                        {
                            Url = mediaUrl,
                            Referer = "https://novel.snssdk.com/",
                            Quality = quality,
                            DecryptionKey = key,
                        },
                        quality
                    )
                );
            }
            catch (Exception exception) when (exception is PhoenixReadApiException)
            {
                keyError = exception;
            }
        }

        if (choices.Count == 0)
        {
            if (keyError is not null)
            {
                throw keyError;
            }

            throw new PhoenixReadApiMediaException(
                "备用播放接口未返回可用媒体和密钥。",
                "playback"
            );
        }

        choices.Sort(static (left, right) => right.Score.CompareTo(left.Score));
        PlaybackMedia selected = choices[0].Media;
        return new PlaybackMedia
        {
            Url = selected.Url,
            Referer = selected.Referer,
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

    private static List<string> MediaAddresses(AppMediaVariantDto info)
    {
        List<string> addresses = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (
            var raw in info
                .MainUrls.Concat(info.BackupUrls)
                .Concat(info.BackupUrls1)
                .Concat(info.BackupUrls2)
                .Concat(info.BackupUrlCollection)
                .Concat(info.UrlList)
        )
        {
            Add(raw);
        }
        return addresses;

        void Add(string? value)
        {
            var text = value?.Trim() ?? string.Empty;
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

    private static List<string> MediaAddresses(WebPlayerInfoDto info)
    {
        List<string> addresses = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (
            var raw in info
                .MainUrls.Concat(info.BackupUrls)
                .Concat(info.BackupUrls1)
                .Concat(info.BackupUrlCollection)
                .Concat(info.UrlList)
        )
        {
            var text = raw.Trim();
            if (!UrlNormalizer.IsHttpUrl(text))
            {
                try
                {
                    text = Encoding.UTF8.GetString(Base64Decoder.Decode(text)).Trim();
                }
                catch (FormatException)
                {
                    continue;
                }
            }

            if (UrlNormalizer.IsHttpUrl(text) && seen.Add(text))
            {
                addresses.Add(text);
            }
        }

        return addresses;
    }

    private readonly record struct ScoredMedia(PlaybackMedia Media, int Score);
}
