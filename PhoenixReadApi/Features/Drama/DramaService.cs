using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Drama;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Contracts.Web.Drama;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Drama;

using Drama = global::PhoenixReadApi.Drama;

/// <summary>加载详情、校验分集归属，并缓存已完成的结果。</summary>
internal sealed partial class DramaService : PhoenixFeatureService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);
    private readonly Lock _stateLock = new();
    private readonly Dictionary<string, CachedValue<DramaDetails>> _cache =
    [
        with(StringComparer.Ordinal),
    ];
    private readonly ConcurrentDictionary<string, Lazy<Task<DramaDetails>>> _pending = new(
        StringComparer.Ordinal
    );

    internal DramaService(PhoenixReadClientRuntime runtime)
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
        Task<string> scoreTask = TryFetchScoreAsync(seriesId, CancellationToken.None);
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
            string score = await scoreTask.ConfigureAwait(false);
            return ParseAppDetails(result.Data, seriesId, score);
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
            string score = await scoreTask.ConfigureAwait(false);
            return await FetchWebDetailsAsync(seriesId, CancellationToken.None, score)
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

    private static DramaDetails ParseAppDetails(
        AppDetailsData? data,
        string seriesId,
        string extraScore = ""
    )
    {
        DramaDto detail = data?.VideoData ?? new();
        if (ValueNormalizer.First(detail.SeriesId, detail.SeriesIdNumber) != seriesId)
        {
            throw new PhoenixReadApiResponseException("App 未返回所请求的剧集。", "detail");
        }

        Drama drama = DramaMapper.FromDto(detail, "短剧", scoreOverride: extraScore);
        List<Episode> episodes = [];
        HashSet<string> usedIds = [with(StringComparer.Ordinal)];
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

            episodes.Add(CreateEpisode(item, episode));
        }

        episodes.Sort(static (left, right) => left.Number.CompareTo(right.Number));
        int total = detail.EpisodeCount;
        if (episodes.Count == 0 || total > episodes.Count)
        {
            throw new PhoenixReadApiResponseException("App 未返回完整且连续的分集。", "detail");
        }

        for (int i = 0; i < episodes.Count; i++)
        {
            if (episodes[i].Number != i + 1)
            {
                throw new PhoenixReadApiResponseException("App 未返回完整且连续的分集。", "detail");
            }
        }

        return new DramaDetails
        {
            Drama = drama,
            Episodes = episodes,
            RelatedAlbumId = detail.EffectiveAlbumId,
            IsMultiSeason = detail.IsMultiSeason,
            SeasonIndex = detail.SeasonIndex > 0 ? detail.SeasonIndex : 1,
            SourceNovel = MapSourceNovel(detail.RelateBook),
        };
    }

    private async Task<DramaDetails> FetchWebDetailsAsync(
        string seriesId,
        CancellationToken cancellationToken,
        string extraScore = ""
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
        WebSeriesDetailDto? detail = page?.SeriesDetail
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
            Drama = new Drama
            {
                Id = seriesId,
                Title = title,
                Score = extraScore,
            },
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

    private static Episode CreateEpisode(DramaEpisodeDto dto, int number) =>
        new()
        {
            VideoId = dto.VideoId,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? $"第{number}集" : dto.Title,
            Number = number,
            Duration =
                dto.DurationSeconds > 0 ? TimeSpan.FromSeconds(dto.DurationSeconds) : TimeSpan.Zero,
            CoverUrl = dto.EffectiveCover,
            Playable = !dto.DisablePlay,
            IsVertical = dto.Vertical,
            IsNewlyUpdated = dto.IsNewlyUpdate,
            CommentCount = dto.CommentCount,
            LikeCount = dto.DiggedCount,
        };

    internal async Task<IReadOnlyList<DramaDetails>> GetBatchDramaDetailsWithEpisodesAsync(
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

        Dictionary<string, string> batchScores = await TryFetchBatchScoresAsync(
                ids,
                cancellationToken
            )
            .ConfigureAwait(false);

        Dictionary<string, DramaDetails> found = [with(StringComparer.Ordinal)];
        const int batchSize = 10;
        List<Task<AppResponse<Dictionary<string, AppPlayerSeriesEntry>>>> tasks = [];
        for (int i = 0; i < ids.Count; i += batchSize)
        {
            int count = Math.Min(batchSize, ids.Count - i);
            List<string> chunk = ids.GetRange(i, count);
            AppPlayerBatchDetailPayload payload = new()
            {
                SeriesId = string.Join(",", chunk),
                Scene = "player",
                BizParam = new AppPlayerBatchBizParam { Source = 7 },
            };

            tasks.Add(
                _appTransport.PostAsync<
                    AppPlayerBatchDetailPayload,
                    Dictionary<string, AppPlayerSeriesEntry>
                >(
                    "/novel/player/multi_video_detail/player/v1",
                    payload,
                    commentRequest: false,
                    cancellationToken
                )
            );
        }

        AppResponse<Dictionary<string, AppPlayerSeriesEntry>>[] responses = await Task.WhenAll(
                tasks
            )
            .ConfigureAwait(false);

        foreach (AppResponse<Dictionary<string, AppPlayerSeriesEntry>> response in responses)
        {
            Dictionary<string, AppPlayerSeriesEntry> entries = response.Data ?? [];
            foreach ((string sid, AppPlayerSeriesEntry entry) in entries)
            {
                DramaDto videoData = entry.VideoData;
                string score = batchScores.GetValueOrDefault(sid, string.Empty);
                Drama drama = DramaMapper.FromDto(videoData, "短剧", scoreOverride: score);
                if (string.IsNullOrWhiteSpace(drama.Id))
                {
                    continue;
                }

                List<Episode> episodes = [];
                HashSet<int> seenEpisodes = [];
                foreach (DramaEpisodeDto epDto in videoData.VideoList)
                {
                    if (
                        ValueNormalizer.IsNumericId(epDto.VideoId)
                        && epDto.Episode > 0
                        && seenEpisodes.Add(epDto.Episode)
                    )
                    {
                        episodes.Add(CreateEpisode(epDto, epDto.Episode));
                    }
                }

                episodes.Sort(static (left, right) => left.Number.CompareTo(right.Number));
                found[sid] = new DramaDetails { Drama = drama, Episodes = episodes };
            }
        }

        List<DramaDetails> result = [];
        foreach (string id in ids)
        {
            if (found.TryGetValue(id, out DramaDetails? details))
            {
                result.Add(details);
            }
        }

        return result;
    }

    private async Task<string> TryFetchScoreAsync(
        string seriesId,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, string> scores = await TryFetchBatchScoresAsync(
                [seriesId],
                cancellationToken
            )
            .ConfigureAwait(false);
        return scores.GetValueOrDefault(seriesId, string.Empty);
    }

    private async Task<Dictionary<string, string>> TryFetchBatchScoresAsync(
        IReadOnlyList<string> seriesIds,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, string> result = [with(StringComparer.Ordinal)];
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
            return result;
        }

        try
        {
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

            if (response.Data is not null)
            {
                foreach (AppMultiDetailItem item in response.Data)
                {
                    string id = item.BookId.Trim();
                    string score = ValueNormalizer.First(item.FinalScoreV2, item.Score);
                    if (id.Length > 0 && score.Length > 0)
                    {
                        result[id] = score;
                    }
                }
            }
        }
        catch
        {
            // 非核心元数据补齐失败不阻断主流程
        }

        return result;
    }

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

    internal async Task<DramaAlbum> GetAlbumAsync(
        string albumId,
        CancellationToken cancellationToken
    )
    {
        albumId = (albumId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(albumId))
        {
            throw new ArgumentException("合集 ID 不能为空。", nameof(albumId));
        }

        AppAlbumPayload payload = new() { AlbumId = albumId, NeedVideoDetailInfo = true };

        AppResponse<AppAlbumData> response = await _appTransport
            .PostAsync<AppAlbumPayload, AppAlbumData>(
                "/novel/player/album_detail/v1/",
                payload,
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);

        AppAlbumModel? model = response.Data?.AlbumData;
        if (
            model is null
            || (string.IsNullOrWhiteSpace(model.EffectiveId) && model.EffectiveItems.Count == 0)
        )
        {
            throw new PhoenixReadApiResponseException("合集信息不存在或暂不可用。", "album");
        }

        List<DramaSeason> seasons = [];
        foreach (AppAlbumSeasonItemDto item in model.EffectiveItems)
        {
            string sid = item.EffectiveSeriesId;
            if (string.IsNullOrWhiteSpace(sid))
            {
                continue;
            }

            seasons.Add(
                new DramaSeason
                {
                    DramaId = sid,
                    Title = item.Title,
                    Index = item.SeasonIndex > 0 ? item.SeasonIndex : seasons.Count + 1,
                    CoverUrl = item.Cover,
                    EpisodeCount = item.EpisodeCount,
                    Status =
                        item.SeriesStatus == 1
                            ? DramaReleaseStatus.Finished
                            : DramaReleaseStatus.Ongoing,
                }
            );
        }

        seasons.Sort(static (a, b) => a.Index.CompareTo(b.Index));

        return new DramaAlbum
        {
            Id = !string.IsNullOrWhiteSpace(model.EffectiveId) ? model.EffectiveId : albumId,
            Title = model.Title,
            Description = model.Intro,
            CoverUrl = model.Cover,
            SeasonCount = model.SeasonCount > 0 ? model.SeasonCount : seasons.Count,
            Seasons = seasons,
        };
    }

    internal async Task<DramaAlbum> GetDramaSeasonsAsync(
        string seriesId,
        CancellationToken cancellationToken
    )
    {
        DramaDetails details = await GetAsync(seriesId, cancellationToken).ConfigureAwait(false);

        // 1. 优先尝试从 plan feed 中获取系列剧行 (show_type 547)
        try
        {
            AppPlanResponse plan = await _appTransport
                .GetRootAsync<AppPlanResponse>(
                    "/reading/bookapi/plan/v1/",
                    [
                        new("book_id", seriesId),
                        new("from", "single_col_same_ip_series"),
                        new("scene", "10"),
                        new("session_uuid", Guid.NewGuid().ToString("N")),
                        new("filter_ids", seriesId),
                        new("user_is_login", "0"),
                    ],
                    cancellationToken
                )
                .ConfigureAwait(false);

            AppPlanCellDto? seasonsCell = plan.Data.FirstOrDefault(static c =>
                c is { ShowType: 547, EffectiveItems.Count: > 0 }
            );
            if (seasonsCell is not null && seasonsCell.EffectiveItems.Count > 0)
            {
                List<DramaSeason> seasons = [];
                foreach (DramaDto item in seasonsCell.EffectiveItems)
                {
                    string sid = ValueNormalizer.First(
                        item.SeriesId,
                        item.SeriesIdNumber,
                        item.BookId
                    );
                    if (string.IsNullOrWhiteSpace(sid))
                    {
                        continue;
                    }

                    seasons.Add(
                        new DramaSeason
                        {
                            DramaId = sid,
                            Title = ValueNormalizer.First(item.SeriesTitle, item.Title, item.Name),
                            Index = item.SeasonIndex > 0 ? item.SeasonIndex : seasons.Count + 1,
                            CoverUrl = ValueNormalizer.First(item.Cover, item.SeriesCover),
                            EpisodeCount = item.EpisodeCount,
                            Status =
                                sid == seriesId
                                    ? details.Drama.ReleaseStatus
                                    : DramaReleaseStatus.Unknown,
                        }
                    );
                }

                if (seasons.Count > 0)
                {
                    seasons.Sort(static (a, b) => a.Index.CompareTo(b.Index));
                    return new DramaAlbum
                    {
                        Id = details.RelatedAlbumId,
                        Title = ExtractBaseTitle(details.Drama.Title),
                        SeasonCount = seasons.Count,
                        Seasons = seasons,
                    };
                }
            }
        }
        catch (Exception)
        {
            // Plan feed 降级
        }

        // 2. 如果存在合集 ID，尝试请求合集接口
        if (!string.IsNullOrWhiteSpace(details.RelatedAlbumId))
        {
            try
            {
                DramaAlbum album = await GetAlbumAsync(details.RelatedAlbumId, cancellationToken)
                    .ConfigureAwait(false);
                if (album.Seasons.Count > 0)
                {
                    return album;
                }
            }
            catch (Exception)
            {
                // 合集接口降级
            }
        }

        // 3. 基础作品单季快速返回
        string baseTitle = ExtractBaseTitle(details.Drama.Title);
        return new DramaAlbum
        {
            Id = details.RelatedAlbumId,
            Title = baseTitle,
            Description = details.Drama.Description,
            CoverUrl = details.Drama.CoverUrl,
            SeasonCount = 1,
            Seasons =
            [
                new DramaSeason
                {
                    DramaId = seriesId,
                    Title = details.Drama.Title,
                    Index = details.SeasonIndex > 0 ? details.SeasonIndex : 1,
                    CoverUrl = details.Drama.CoverUrl,
                    EpisodeCount = details.Episodes.Count,
                    Status = details.Drama.ReleaseStatus,
                },
            ],
        };
    }

    internal async Task<DramaRecommendations> GetDramaRecommendationsAsync(
        string seriesId,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        AppPlanResponse plan = await _appTransport
            .GetRootAsync<AppPlanResponse>(
                "/reading/bookapi/plan/v1/",
                [
                    new("book_id", seriesId),
                    new("from", "select_panel_recommend"),
                    new("scene", "10"),
                    new("session_uuid", Guid.NewGuid().ToString("N")),
                    new("user_is_login", "0"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);

        AppPlanCellDto? targetCell = plan.Data.FirstOrDefault(static c =>
            c is { ShowType: 160, EffectiveItems.Count: > 0 }
        ) ?? plan.Data.FirstOrDefault(static c => c.EffectiveItems.Count > 0);

        if (targetCell is null || targetCell.EffectiveItems.Count == 0)
        {
            throw new PhoenixReadApiResponseException("暂无该剧的关联推荐内容。", "recommend");
        }

        List<Drama> items = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        foreach (DramaDto dto in targetCell.EffectiveItems)
        {
            Drama drama = DramaMapper.FromDto(dto, "短剧");
            if (drama.Id.Length > 0 && seen.Add(drama.Id))
            {
                items.Add(drama);
            }
        }

        string cellTitle = !string.IsNullOrWhiteSpace(targetCell.CellName)
            ? targetCell.CellName
            : "更多好剧";
        string sessionId = !string.IsNullOrWhiteSpace(targetCell.SessionId)
            ? targetCell.SessionId
            : plan.SessionId;

        return new DramaRecommendations
        {
            Title = cellTitle,
            SessionId = sessionId,
            Items = items,
        };
    }

    private static Novel? MapSourceNovel(DramaRelateBookContainerDto? container)
    {
        DramaRelateBookInfoDto? info = container?.BookInfo;
        if (info is null || string.IsNullOrWhiteSpace(info.BookId) || info.BookId == "0")
        {
            return null;
        }

        NovelCreationStatus status = info.CreationStatus switch
        {
            "1" => NovelCreationStatus.Ongoing,
            "2" => NovelCreationStatus.Completed,
            _ => NovelCreationStatus.Unknown,
        };

        IReadOnlyList<string> tags = string.IsNullOrWhiteSpace(info.Tags)
            ? []
            :
            [
                .. info.Tags.Split(
                    [',', '，', ' '],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                ),
            ];

        return new Novel
        {
            Id = info.BookId,
            Title = info.BookName,
            ShortTitle = info.BookShortName,
            Status = status,
            CoverUrl = info.ThumbUrl,
            ReadCount = info.ReadCount,
            Tags = tags,
        };
    }

    private static string ExtractBaseTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        string cleaned = ChineseSeasonMarkerRegex().Replace(title, string.Empty).Trim();
        cleaned = EnglishSeasonMarkerRegex().Replace(cleaned, string.Empty).Trim();
        return cleaned.Length > 0 ? cleaned : title;
    }

    private sealed record CachedValue<T>(T Value, DateTimeOffset ExpiresAt);

    [GeneratedRegex(
        @"[\(（]?\s*第[0-9一二三四五六七八九十\d]+[季部篇]\s*[\)）]?",
        RegexOptions.IgnoreCase,
        "zh-CN"
    )]
    private static partial Regex ChineseSeasonMarkerRegex();

    [GeneratedRegex(@"[\(（]?\s*Season\s*\d+\s*[\)）]?", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex EnglishSeasonMarkerRegex();
}
