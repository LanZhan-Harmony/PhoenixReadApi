using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Drama;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Contracts.Web.Drama;
using PhoenixReadApi.Internal.Features.Search;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Drama;

using Drama = PhoenixReadApi.Drama;
using Post = PhoenixReadApi.Post;

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

    internal async Task<DramaDetails> GetAsync(string dramaId, CancellationToken cancellationToken)
    {
        string normalizedId = NormalizeDramaId(dramaId);
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

    private async Task<DramaDetails> LoadAsync(string dramaId)
    {
        Task<string> scoreTask = TryFetchScoreAsync(dramaId, CancellationToken.None);
        Exception? appError;
        try
        {
            AppResponse<AppDetailsData> result = await _appTransport
                .PostAsync<AppDetailPayload, AppDetailsData>(
                    "/novel/player/video_detail/v1/",
                    new AppDetailPayload { SeriesId = dramaId },
                    commentRequest: false,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
            string score = await scoreTask.ConfigureAwait(false);
            return ParseAppDetails(result.Data, dramaId, score);
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
            return await FetchWebDetailsAsync(dramaId, CancellationToken.None, score)
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
        string dramaId,
        string extraScore = ""
    )
    {
        DramaDto detail = data?.VideoData ?? new();
        if (ValueNormalizer.First(detail.SeriesId, detail.SeriesIdNumber) != dramaId)
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

            string returnedDramaId = item.SeriesId.Trim();
            if (returnedDramaId.Length > 0 && returnedDramaId != dramaId)
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
            AlbumId = detail.EffectiveAlbumId,
            IsMultiSeason = detail.IsMultiSeason,
            SeasonIndex = detail.SeasonIndex > 0 ? detail.SeasonIndex : 1,
            SourceNovel = MapSourceNovel(detail.RelateBook),
        };
    }

    private async Task<DramaDetails> FetchWebDetailsAsync(
        string dramaId,
        CancellationToken cancellationToken,
        string extraScore = ""
    )
    {
        Uri uri = new(_options.WebBaseUri, "detail?series_id=" + Uri.EscapeDataString(dramaId));
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
            dramaId
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
                Id = dramaId,
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
        IEnumerable<string> dramaIds,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(dramaIds);
        List<string> ids =
        [
            .. dramaIds
                .Select(NormalizeDramaId)
                .Where(static id => id.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Take(50),
        ];

        if (ids.Count == 0)
        {
            return [];
        }

        Task<Dictionary<string, string>> batchScoresTask = TryFetchBatchScoresAsync(
            ids,
            cancellationToken
        );

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

        Dictionary<string, string> batchScores = await batchScoresTask.ConfigureAwait(false);

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
        string dramaId,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, string> scores = await TryFetchBatchScoresAsync(
                [dramaId],
                cancellationToken
            )
            .ConfigureAwait(false);
        return scores.GetValueOrDefault(dramaId, string.Empty);
    }

    private async Task<Dictionary<string, string>> TryFetchBatchScoresAsync(
        IReadOnlyList<string> dramaIds,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, string> result = [with(StringComparer.Ordinal)];
        List<string> ids =
        [
            .. dramaIds
                .Select(NormalizeDramaId)
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
        IEnumerable<string> dramaIds,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(dramaIds);
        List<string> ids =
        [
            .. dramaIds
                .Select(NormalizeDramaId)
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
                        item.EffectiveSeriesStatus == 1
                            ? DramaReleaseStatus.Finished
                            : DramaReleaseStatus.Ongoing,
                }
            );
        }

        seasons.Sort(static (a, b) => a.Index.CompareTo(b.Index));

        return new DramaAlbum
        {
            Id = !string.IsNullOrWhiteSpace(model.EffectiveId) ? model.EffectiveId : albumId,
            Title = model.EffectiveTitle,
            Description = model.EffectiveIntro,
            CoverUrl = model.EffectiveCover,
            SeasonCount = model.SeasonCount > 0 ? model.SeasonCount : seasons.Count,
            Seasons = seasons,
        };
    }

    internal async Task<DramaAlbum> GetDramaSeasonsAsync(
        string dramaId,
        CancellationToken cancellationToken
    )
    {
        DramaDetails details = await GetAsync(dramaId, cancellationToken).ConfigureAwait(false);

        // 1. 优先尝试从 plan feed 中获取系列剧行 (show_type 547)
        try
        {
            AppPlanResponse plan = await _appTransport
                .GetRootAsync<AppPlanResponse>(
                    "/reading/bookapi/plan/v1/",
                    [
                        new("book_id", dramaId),
                        new("from", "single_col_same_ip_series"),
                        new("scene", "10"),
                        new("session_uuid", Guid.NewGuid().ToString("N")),
                        new("filter_ids", dramaId),
                        new("user_is_login", UserIsLogin),
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
                                sid == dramaId
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
                        Id = details.AlbumId,
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
        if (!string.IsNullOrWhiteSpace(details.AlbumId))
        {
            try
            {
                DramaAlbum album = await GetAlbumAsync(details.AlbumId, cancellationToken)
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
            Id = details.AlbumId,
            Title = baseTitle,
            Description = details.Drama.Description,
            CoverUrl = details.Drama.CoverUrl,
            SeasonCount = 1,
            Seasons =
            [
                new DramaSeason
                {
                    DramaId = dramaId,
                    Title = details.Drama.Title,
                    Index = details.SeasonIndex > 0 ? details.SeasonIndex : 1,
                    CoverUrl = details.Drama.CoverUrl,
                    EpisodeCount = details.Episodes.Count,
                    Status = details.Drama.ReleaseStatus,
                },
            ],
        };
    }

    internal async Task<IReadOnlyList<RelatedWorkItem>> GetDramaRelatedWorksAsync(
        string dramaId,
        CancellationToken cancellationToken
    )
    {
        dramaId = NormalizeDramaId(dramaId);
        AppPlanResponse plan = await _appTransport
            .GetRootAsync<AppPlanResponse>(
                "/reading/bookapi/plan/v1/",
                [
                    new("book_id", dramaId),
                    new("from", "detail_page_more_related"),
                    new("scene", "10"),
                    new("session_uuid", Guid.NewGuid().ToString("D")),
                    new("user_is_login", UserIsLogin),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);

        AppPlanCellDto? relatedCell = plan.Data.FirstOrDefault(static c => c.ShowType == 547);
        List<RelatedWorkItem> relatedWorks = [];
        HashSet<string> seenRelated = [with(StringComparer.Ordinal)];
        if (relatedCell is not null)
        {
            foreach (DramaDto dto in relatedCell.VideoData)
            {
                Drama drama = DramaMapper.FromDto(dto, "短剧");
                if (drama.Id.Length > 0 && seenRelated.Add("d:" + drama.Id))
                {
                    relatedWorks.Add(drama);
                }
            }

            foreach (AppPlanBookDto book in relatedCell.BookData)
            {
                Novel novel = MapPlanBook(book);
                if (novel.Id.Length > 0 && seenRelated.Add("n:" + novel.Id))
                {
                    relatedWorks.Add(novel);
                }
            }
        }

        return relatedWorks;
    }

    internal async Task<RecommendationFeedPage> GetDramaGuessYouLikeAsync(
        string dramaId,
        int offset,
        string cellId,
        string sessionId,
        int limit,
        CancellationToken cancellationToken
    )
    {
        dramaId = NormalizeDramaId(dramaId);
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "偏移量不能为负数。");
        }
        if (limit is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "每页数量必须在 1 到 50 之间。");
        }

        if (offset == 0 && string.IsNullOrWhiteSpace(cellId))
        {
            AppPlanResponse plan = await _appTransport
                .GetRootAsync<AppPlanResponse>(
                    "/reading/bookapi/plan/v1/",
                    [
                        new("book_id", dramaId),
                        new("from", "detail_page_more_related"),
                        new("scene", "10"),
                        new("session_uuid", Guid.NewGuid().ToString("D")),
                        new("user_is_login", UserIsLogin),
                    ],
                    cancellationToken
                )
                .ConfigureAwait(false);

            AppPlanCellDto? guessCell = plan.Data.FirstOrDefault(static c => c.ShowType == 506);
            string resolvedSessionId = ValueNormalizer.First(
                guessCell?.SessionId,
                plan.SessionId
            );
            return guessCell is not null
                ? MapFeedPage(guessCell, guessCell.NextOffset, guessCell.HasMore, resolvedSessionId)
                : new RecommendationFeedPage { Title = "猜你喜欢", SessionId = resolvedSessionId };
        }

        cellId = NormalizeVideoId(cellId);
        List<KeyValuePair<string, string>> query =
        [
            new("cell_id", cellId),
            new("client_req_type", "2"),
            new("limit", limit.ToString(CultureInfo.InvariantCulture)),
            new("offset", offset.ToString(CultureInfo.InvariantCulture)),
            new("related_book_id", dramaId),
            new("session_uuid", Guid.NewGuid().ToString("D")),
        ];
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            query.Add(new("session_id", sessionId.Trim()));
        }

        AppResponse<AppPlanCellChangeData> response = await _appTransport
            .GetAsync<AppPlanCellChangeData>(
                "/reading/bookapi/bookmall/cell/change/v1/",
                query,
                cancellationToken
            )
            .ConfigureAwait(false);
        AppPlanCellChangeData data = response.Data ?? new();
        AppPlanCellDto cell = data.CellView ?? new();
        return MapFeedPage(
            cell,
            data.NextOffset,
            data.HasMore,
            !string.IsNullOrWhiteSpace(data.SessionId) ? data.SessionId : sessionId
        );
    }

    internal async Task<Drama?> GetPlaybackEndRecommendationAsync(
        string id,
        bool isAlbum,
        CancellationToken cancellationToken
    )
    {
        id = NormalizeDramaId(id);
        AppPlanResponse plan = await _appTransport
            .GetRootAsync<AppPlanResponse>(
                "/reading/bookapi/plan/v1/",
                [
                    new("book_id", id),
                    new("filter_ids", id),
                    new("from", isAlbum ? "series_end_recommend" : "series_end_bottom_bar"),
                    new("scene", "10"),
                    new("user_is_login", UserIsLogin),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);

        DramaDto? first = plan.Data.SelectMany(static c => c.EffectiveItems).FirstOrDefault();
        if (first is null)
        {
            return null;
        }

        Drama drama = DramaMapper.FromDto(first, "短剧");
        return drama.Id.Length > 0 ? drama : null;
    }

    private static RecommendationFeedPage MapFeedPage(
        AppPlanCellDto cell,
        int nextOffset,
        bool hasMore,
        string sessionId
    )
    {
        List<RecommendationFeedItem> items = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        foreach (AppPlanFeedCellDto card in cell.CellData)
        {
            if (card.ShowType == 557 || card.PostData.Count > 0)
            {
                foreach (AppSearchPostDto postDto in card.PostData)
                {
                    Post post = SearchService.MapSearchPost(postDto);
                    if (post.Id.Length > 0 && seen.Add("p:" + post.Id))
                    {
                        items.Add(post);
                    }
                }
            }
            else if (card.VideoData.Count > 0)
            {
                foreach (DramaDto dramaDto in card.VideoData)
                {
                    Drama drama = DramaMapper.FromDto(dramaDto, "短剧");
                    if (drama.Id.Length > 0 && seen.Add("d:" + drama.Id))
                    {
                        items.Add(drama);
                    }
                }
            }
        }

        return new RecommendationFeedPage
        {
            Title = !string.IsNullOrWhiteSpace(cell.CellName) ? cell.CellName : "猜你喜欢",
            CellId = cell.EffectiveCellId,
            SessionId = sessionId,
            NextOffset = nextOffset,
            HasMore = hasMore,
            Items = items,
        };
    }

    private static Novel MapPlanBook(AppPlanBookDto book)
    {
        NovelCreationStatus status = book.CreationStatus switch
        {
            "1" => NovelCreationStatus.Ongoing,
            "2" => NovelCreationStatus.Completed,
            _ => NovelCreationStatus.Unknown,
        };

        IReadOnlyList<string> tags = string.IsNullOrWhiteSpace(book.Tags)
            ? []
            : [.. book.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

        return new Novel
        {
            Id = book.BookId,
            Title = book.BookName,
            Author = book.Author,
            Description = book.Abstract,
            Status = status,
            CoverUrl = book.ThumbUrl,
            ReadCount = book.ReadCount,
            SerialCount = book.SerialCount,
            Tags = tags,
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
