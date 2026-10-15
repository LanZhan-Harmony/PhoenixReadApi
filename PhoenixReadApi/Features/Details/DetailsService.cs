using System.Collections.Concurrent;
using System.Text.Json;
using PhoenixReadApi.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.Web;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Details;

/// <summary>Loads details, validates episode ownership and caches completed results.</summary>
internal sealed class DetailsService : PhoenixFeatureService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);
    private readonly Lock _stateLock = new();
    private readonly Dictionary<string, CachedValue<DramaDetails>> _cache = new(
        StringComparer.Ordinal
    );
    private readonly ConcurrentDictionary<string, Lazy<Task<DramaDetails>>> _pending = new(
        StringComparer.Ordinal
    );

    internal DetailsService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<DramaDetails> GetAsync(string seriesId, CancellationToken cancellationToken)
    {
        var normalizedId = NormalizeSeriesId(seriesId);
        if (TryGetCache(normalizedId, out DramaDetails? cached) && cached is not null)
        {
            return cached;
        }

        Lazy<Task<DramaDetails>> pending = _pending.GetOrAdd(
            normalizedId,
            static (id, state) =>
                new Lazy<Task<DramaDetails>>(
                    () => state.LoadAsync(id),
                    LazyThreadSafetyMode.ExecutionAndPublication
                ),
            this
        );
        try
        {
            DramaDetails result = await pending
                .Value.WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            lock (_stateLock)
            {
                _cache[normalizedId] = new CachedValue<DramaDetails>(
                    result,
                    DateTimeOffset.UtcNow.Add(CacheLifetime)
                );
            }

            return result;
        }
        finally
        {
            _pending.TryRemove(
                new KeyValuePair<string, Lazy<Task<DramaDetails>>>(normalizedId, pending)
            );
        }
    }

    private async Task<DramaDetails> LoadAsync(string seriesId)
    {
        Exception? appError;
        try
        {
            AppResponse<AppDetailsData> result = await _appTransport
                .PostAsync<AppDetailPayload, AppDetailsData>(
                    "/novel/player/video_detail/v1/",
                    new AppDetailPayload { SeriesId = seriesId },
                    commentRequest: false,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
            return ParseAppDetails(result.Data, seriesId);
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            appError = exception;
        }

        if (!_options.EnableWebFallback)
        {
            throw ToApiException("App 详情获取失败。", "detail", appError);
        }

        try
        {
            return await FetchWebDetailsAsync(seriesId, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception webException) when (IsRecoverable(webException))
        {
            throw new PhoenixReadApiFallbackException(
                $"App 详情失败：{PublicError(appError!)}；网页详情失败：{PublicError(webException)}",
                "detail",
                webException
            );
        }
    }

    private static DramaDetails ParseAppDetails(AppDetailsData? data, string seriesId)
    {
        AppDramaDto? detail = data?.VideoData;
        if (
            detail is null
            || JsonValueReader.First(detail.SeriesId, detail.SeriesIdNumber) != seriesId
        )
        {
            throw new PhoenixReadApiResponseException("App 未返回所请求的剧集。", "detail");
        }

        Drama drama = DramaMapper.FromDto(detail, "短剧");
        List<Chapter> chapters = [];
        HashSet<string> usedIds = new(StringComparer.Ordinal);
        HashSet<int> usedEpisodes = [];
        foreach (AppVideoDto item in data!.VideoList)
        {
            var videoId = item.VideoId?.Trim() ?? string.Empty;
            var episode = item.Episode;
            if (
                !JsonValueReader.IsNumericId(videoId)
                || episode < 1
                || !usedIds.Add(videoId)
                || !usedEpisodes.Add(episode)
            )
            {
                throw new PhoenixReadApiResponseException("App 分集编号或视频 ID 无效。", "detail");
            }

            var returnedSeriesId = item.SeriesId?.Trim() ?? string.Empty;
            if (returnedSeriesId.Length > 0 && returnedSeriesId != seriesId)
            {
                throw new PhoenixReadApiResponseException("App 返回了其他剧集的分集。", "detail");
            }

            chapters.Add(CreateChapter(seriesId, videoId, episode));
        }

        chapters.Sort(static (left, right) => left.Episode.CompareTo(right.Episode));
        var total = detail.EpisodeCount;
        if (
            chapters.Count == 0
            || total > chapters.Count
            || chapters.Where((chapter, index) => chapter.Episode != index + 1).Any()
        )
        {
            throw new PhoenixReadApiResponseException("App 未返回完整且连续的分集。", "detail");
        }

        return new DramaDetails { Drama = drama, Chapters = chapters };
    }

    private async Task<DramaDetails> FetchWebDetailsAsync(
        string seriesId,
        CancellationToken cancellationToken
    )
    {
        Uri uri = new(_options.WebBaseUri, "detail?series_id=" + Uri.EscapeDataString(seriesId));
        using JsonDocument? document = RouterDataExtractor.Parse(
            await GetTextAsync(uri, _options.WebBaseUri, "detail", cancellationToken)
                .ConfigureAwait(false)
        );
        WebDetailsPageDto? page = RouterDataLocator.Deserialize<WebDetailsPageDto>(
            document,
            "detail_page",
            "detail_"
        );
        WebSeriesDetailDto? detail =
            (page?.SeriesDetail)
            ?? throw new PhoenixReadApiResponseException("网页详情为空。", "detail");
        var title = JsonValueReader.First(
            detail.SeriesName,
            detail.SeriesTitle,
            detail.Name,
            seriesId
        );
        List<Chapter> chapters = [];
        var index = 1;
        foreach (var item in detail.VideoIds)
        {
            var videoId = item.Trim();
            if (!JsonValueReader.IsNumericId(videoId))
            {
                continue;
            }

            chapters.Add(CreateChapter(seriesId, videoId, index++));
        }

        if (chapters.Count == 0)
        {
            throw new PhoenixReadApiResponseException("网页详情没有返回剧集 ID。", "detail");
        }

        return new DramaDetails
        {
            Drama = new Drama
            {
                Id = "hongguo:" + seriesId,
                SourceId = seriesId,
                Source = PhoenixReadApiClient.SourceName,
                Title = title,
                Name = title,
                Channel = "红果",
            },
            Chapters = chapters,
        };
    }

    private bool TryGetCache(string key, out DramaDetails? value)
    {
        lock (_stateLock)
        {
            if (_cache.TryGetValue(key, out CachedValue<DramaDetails>? cached))
            {
                if (cached.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    value = cached.Value;
                    return true;
                }

                _cache.Remove(key);
            }
        }

        value = null;
        return false;
    }

    private static Chapter CreateChapter(string seriesId, string videoId, int episode) =>
        new()
        {
            Id = $"hongguo:{seriesId}:{videoId}",
            Source = PhoenixReadApiClient.SourceName,
            Title = $"第{episode}集",
            VideoUrl = "hongguo-cenc://" + videoId,
            Episode = episode,
        };

    private sealed record CachedValue<T>(T Value, DateTimeOffset ExpiresAt);
}
