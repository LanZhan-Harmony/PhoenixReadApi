using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Details;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Contracts.Web.Details;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Details;

/// <summary>加载详情、校验分集归属，并缓存已完成的结果。</summary>
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
        string normalizedId = NormalizeSeriesId(seriesId);
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
                $"App 详情失败：{PublicError(appError)}；网页详情失败：{PublicError(webException)}",
                "detail",
                webException
            );
        }
    }

    private static DramaDetails ParseAppDetails(AppDetailsData? data, string seriesId)
    {
        DramaDto detail = data?.VideoData ?? new();
        if (ValueNormalizer.First(detail.SeriesId, detail.SeriesIdNumber) != seriesId)
        {
            throw new PhoenixReadApiResponseException("App 未返回所请求的剧集。", "detail");
        }

        Drama drama = DramaMapper.FromDto(detail, "短剧");
        List<Episode> episodes = [];
        HashSet<string> usedIds = new(StringComparer.Ordinal);
        HashSet<int> usedEpisodes = [];
        foreach (DramaEpisodeDto item in detail.VideoList)
        {
            string videoId = item.VideoId.Trim();
            int episode = item.Episode;
            if (
                !ValueNormalizer.IsNumericId(videoId)
                || episode < 1
                || !usedIds.Add(videoId)
                || !usedEpisodes.Add(episode)
            )
            {
                throw new PhoenixReadApiResponseException("App 分集编号或视频 ID 无效。", "detail");
            }

            string returnedSeriesId = item.SeriesId.Trim();
            if (returnedSeriesId.Length > 0 && returnedSeriesId != seriesId)
            {
                throw new PhoenixReadApiResponseException("App 返回了其他剧集的分集。", "detail");
            }

            episodes.Add(CreateEpisode(videoId, episode));
        }

        episodes.Sort(static (left, right) => left.Number.CompareTo(right.Number));
        int total = detail.EpisodeCount;
        if (
            episodes.Count == 0
            || total > episodes.Count
            || episodes.Where((episode, index) => episode.Number != index + 1).Any()
        )
        {
            throw new PhoenixReadApiResponseException("App 未返回完整且连续的分集。", "detail");
        }

        return new DramaDetails { Drama = drama, Episodes = episodes };
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
        string title = ValueNormalizer.First(
            detail.SeriesName,
            detail.SeriesTitle,
            detail.Name,
            seriesId
        );
        List<Episode> episodes = [];
        int index = 1;
        foreach (string item in detail.VideoIds)
        {
            string videoId = item.Trim();
            if (!ValueNormalizer.IsNumericId(videoId))
            {
                continue;
            }

            episodes.Add(CreateEpisode(videoId, index++));
        }

        if (episodes.Count == 0)
        {
            throw new PhoenixReadApiResponseException("网页详情没有返回剧集 ID。", "detail");
        }

        return new DramaDetails
        {
            Drama = new Drama { Id = seriesId, Title = title },
            Episodes = episodes,
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

    private static Episode CreateEpisode(string videoId, int number) =>
        new()
        {
            VideoId = videoId,
            Title = $"第{number}集",
            Number = number,
        };

    internal async Task<IReadOnlyList<Drama>> GetBatchDramaDetailsAsync(
        IEnumerable<string> seriesIds,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(seriesIds);
        List<string> ids =
        [
            .. seriesIds
                .Select(NormalizeSeriesId)
                .Where(static id => id.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Take(50),
        ];

        if (ids.Count == 0)
        {
            return [];
        }

        string joinedIds = string.Join(",", ids);
        List<KeyValuePair<string, string>> query =
        [
            new("book_id", joinedIds),
            new("book_ids", joinedIds),
        ];
        foreach (string id in ids)
        {
            query.Add(new("book_id", id));
        }

        AppResponse<IReadOnlyList<AppMultiDetailItem>> response = await _appTransport
            .GetAsync<IReadOnlyList<AppMultiDetailItem>>(
                "/reading/bookapi/multi-detail/v1/",
                query,
                cancellationToken
            )
            .ConfigureAwait(false);

        IReadOnlyList<AppMultiDetailItem> items = response.Data ?? [];
        return [.. items.Select(MapMultiDetailItem)];
    }

    private static Drama MapMultiDetailItem(AppMultiDetailItem item)
    {
        int episodeCount;
        if (
            !int.TryParse(
                item.DramaChapterNumber,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out episodeCount
            )
            && !int.TryParse(
                item.ChapterNumber,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out episodeCount
            )
        )
        {
            _ = int.TryParse(
                item.SerialCount,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out episodeCount
            );
        }

        string cover = ValueNormalizer.First(
            item.ExpandThumbUrl,
            item.ThumbUrl,
            item.BookshelfThumbUrl
        );
        string desc = ValueNormalizer.First(item.BookAbstractV2, item.Abstract);
        string score = ValueNormalizer.First(item.FinalScoreV2, item.Score);

        List<string> tags = [];
        if (!string.IsNullOrWhiteSpace(item.Category))
        {
            tags.Add(item.Category.Trim());
        }
        if (!string.IsNullOrWhiteSpace(item.Tags))
        {
            tags.AddRange(
                item.Tags.Split(
                    [',', '，', ' ', ';'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
            );
        }

        return new Drama
        {
            Id = item.BookId.Trim(),
            Title = item.BookName.Trim(),
            Description = desc.Trim(),
            CoverUrl = cover.Trim(),
            Category = item.Category.Trim(),
            Remark = item.SubInfo.Trim(),
            Score = score.Trim(),
            EpisodeCount = Math.Max(0, episodeCount),
            Tags = [.. tags.Distinct(StringComparer.Ordinal)],
        };
    }

    private sealed record CachedValue<T>(T Value, DateTimeOffset ExpiresAt);
}
