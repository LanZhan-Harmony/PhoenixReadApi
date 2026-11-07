using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Contracts.Web.Catalog;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Catalog;

/// <summary>协调剧库数据源、分页状态、结果合并和网页兜底。</summary>
internal sealed class CatalogService : PhoenixFeatureService
{
    private const int CatalogPageSize = 18;
    private static readonly TimeSpan CursorLifetime = TimeSpan.FromMinutes(30);
    private readonly Lock _stateLock = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, CatalogCursor> _cursors = new(StringComparer.Ordinal);

    internal CatalogService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<IReadOnlyList<Drama>> GetAsync(
        PhoenixReadApiCategory category,
        CancellationToken cancellationToken
    )
    {
        ValidateCategory(category);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<string> warnings = [];
            List<Drama> items = [];
            bool usedWebFallback = category == PhoenixReadApiCategory.Comic;
            if (category != PhoenixReadApiCategory.Comic)
            {
                try
                {
                    PhoenixReadApiCategory[] categories =
                        category == PhoenixReadApiCategory.All
                            ?
                            [
                                PhoenixReadApiCategory.RealDrama,
                                PhoenixReadApiCategory.ComicDrama,
                                PhoenixReadApiCategory.AiDrama,
                            ]
                            : [category];
                    foreach (PhoenixReadApiCategory appCategory in categories)
                    {
                        items.AddRange(
                            await FetchAppCategoryAsync(appCategory, cancellationToken)
                                .ConfigureAwait(false)
                        );
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (IsRecoverable(exception))
                {
                    warnings.Add(PublicError(exception));
                    usedWebFallback = true;
                }
            }

            if (items.Count == 0 || usedWebFallback)
            {
                (string, string)[] routes = category switch
                {
                    PhoenixReadApiCategory.RealDrama => [("real-drama", "真人剧")],
                    PhoenixReadApiCategory.ComicDrama => [("comic-drama", "漫剧")],
                    PhoenixReadApiCategory.AiDrama => [("ai-drama", "AI剧")],
                    PhoenixReadApiCategory.Comic => [("comic", "动漫")],
                    _ =>
                    [
                        ("real-drama", "真人剧"),
                        ("comic-drama", "漫剧"),
                        ("ai-drama", "AI剧"),
                        ("comic", "动漫"),
                    ],
                };
                try
                {
                    List<Drama> webItems = await FetchWebCatalogAsync(routes, cancellationToken)
                        .ConfigureAwait(false);
                    items = MergeDramas(items, webItems);
                    usedWebFallback = true;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (IsRecoverable(exception))
                {
                    warnings.Add(PublicError(exception));
                    if (items.Count == 0)
                    {
                        throw new PhoenixReadApiFallbackException(
                            "网页剧库获取失败。",
                            "catalog",
                            exception
                        );
                    }
                }
            }

            items = MergeDramas([], items);
            items.Sort(
                static (left, right) =>
                    string.Compare(left.Title, right.Title, StringComparison.Ordinal)
            );
            if (items.Count == 0 && warnings.Count > 0)
            {
                throw new PhoenixReadApiFallbackException(
                    "无法获取红果剧库。" + string.Join("；", warnings),
                    "catalog"
                );
            }

            return items;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<Drama>> FetchAppCategoryAsync(
        PhoenixReadApiCategory category,
        CancellationToken cancellationToken
    )
    {
        (string genre, string scene, string displayName) = CategoryInfo(category);
        CatalogCursor cursor;
        lock (_stateLock)
        {
            _cursors.TryGetValue(genre, out cursor);
        }

        List<Drama> output = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (int page = 0; page < _options.MaxPagesPerCategory && !cursor.Exhausted; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (
                cursor.UpdatedAt != default
                && DateTimeOffset.UtcNow - cursor.UpdatedAt > CursorLifetime
            )
            {
                cursor = cursor with { SessionId = string.Empty };
            }

            AppCatalogPayload payload = new()
            {
                RequestScene = scene,
                Offset = cursor.Offset,
                ClientRequestType = cursor.Offset > 0 ? 2 : 3,
                SessionId = cursor.SessionId,
                SelectItems = new AppCatalogSelectItems { Genre = [genre], Sort = ["online_time"] },
            };
            AppResponse<AppCatalogData> result = await _appTransport
                .PostAsync<AppCatalogPayload, AppCatalogData>(
                    "/reading/distribution/category/landpage/v/",
                    payload,
                    commentRequest: false,
                    cancellationToken
                )
                .ConfigureAwait(false);
            AppCatalogData data =
                result.Data
                ?? throw new PhoenixReadApiResponseException("App 分类数据格式异常。", "catalog");

            List<Drama> pageItems = [];
            foreach (DramaDto row in data.Items)
            {
                Drama drama = DramaMapper.FromDto(row, displayName);
                if (drama.Id.Length > 0)
                {
                    pageItems.Add(drama);
                }
            }

            if (data.Items.Count > 0 && pageItems.Count == 0)
            {
                throw new PhoenixReadApiResponseException(
                    "App 分类未返回可识别的剧集。",
                    "catalog"
                );
            }

            bool hasMore = data.HasMore;
            int nextOffset =
                data.NextOffset > 0 ? data.NextOffset : cursor.Offset + data.Items.Count;
            string pageSignature = string.Join(
                "\n",
                pageItems
                    .Select(static item => item.Id)
                    .OrderBy(static id => id, StringComparer.Ordinal)
            );
            string signature =
                pageSignature.Length == 0
                    ? string.Empty
                    : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pageSignature)));
            if (
                hasMore
                && (
                    pageItems.Count == 0
                    || nextOffset <= cursor.Offset
                    || nextOffset > 1_000_000
                    || signature == cursor.PageSignature
                )
            )
            {
                throw new PhoenixReadApiResponseException("App 分页未前进。", "catalog");
            }

            string session = data.SessionId ?? string.Empty;
            if (session.Length > 4096 || session.Any(static c => c is '\r' or '\n' or '\0'))
            {
                throw new PhoenixReadApiResponseException("App 分页会话无效。", "catalog");
            }

            foreach (Drama drama in pageItems)
            {
                if (seen.Add(drama.Id))
                {
                    output.Add(drama);
                }
            }

            cursor = new CatalogCursor(
                nextOffset,
                session,
                pageItems.LastOrDefault()?.Id ?? string.Empty,
                signature,
                true,
                !hasMore,
                DateTimeOffset.UtcNow
            );
            lock (_stateLock)
            {
                _cursors[genre] = cursor;
            }

            if (!hasMore || data.Items.Count < CatalogPageSize)
            {
                break;
            }
        }

        return output;
    }

    private async Task<List<Drama>> FetchWebCatalogAsync(
        (string Route, string Category)[] routes,
        CancellationToken cancellationToken
    )
    {
        Task<List<Drama>>[] tasks =
        [
            .. routes.Select(route =>
                FetchWebRouteAsync(route.Route, route.Category, cancellationToken)
            ),
        ];
        List<Drama>[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
        List<Drama> output = [];
        foreach (List<Drama> result in results)
        {
            output = MergeDramas(output, result);
        }

        return output;
    }

    private async Task<List<Drama>> FetchWebRouteAsync(
        string route,
        string category,
        CancellationToken cancellationToken
    )
    {
        List<Drama> pages = [];
        for (int page = 1; page <= _options.MaxPagesPerCategory; page++)
        {
            (List<Drama> items, int totalPages) = await FetchWebCategoryPageAsync(
                    route,
                    category,
                    page,
                    cancellationToken
                )
                .ConfigureAwait(false);
            pages = MergeDramas(pages, items);
            if (items.Count == 0 || items.Count < 24 || page >= totalPages)
            {
                break;
            }
        }

        return pages;
    }

    private async Task<(List<Drama> Items, int TotalPages)> FetchWebCategoryPageAsync(
        string route,
        string category,
        int page,
        CancellationToken cancellationToken
    )
    {
        Uri uri = new(
            _options.WebBaseUri,
            $"category/{route}?page={page.ToString(CultureInfo.InvariantCulture)}"
        );
        using JsonDocument? document = RouterDataExtractor.Parse(
            await GetTextAsync(uri, _options.WebBaseUri, "catalog", cancellationToken)
                .ConfigureAwait(false)
        );
        WebCategoryPageDto? data = RouterDataLocator.Deserialize<WebCategoryPageDto>(
            document,
            "category_page",
            "category_$"
        );
        if (data is null || !data.IsSuccess)
        {
            throw new PhoenixReadApiResponseException("网页分类数据不可用。", "catalog");
        }

        List<Drama> output = [];
        foreach (DramaDto item in data.Items)
        {
            Drama drama = DramaMapper.FromDto(item, category);
            if (drama.Id.Length > 0)
            {
                output.Add(drama);
            }
        }

        int total = data.Pagination?.TotalPages ?? 0;
        return (output, Math.Clamp(total <= 0 ? page : total, page, 500));
    }

    internal async Task<IReadOnlyList<TagCategoryGroup>> GetTagCategoriesAsync(
        CancellationToken cancellationToken
    )
    {
        List<KeyValuePair<string, string>> query =
        [
            new("genre_type", "130"),
            new("query_gender", "1"),
            new("category_type", "5"),
            new("page_version", "2"),
            new("source", "detail"),
            new("limit", "12"),
            new("offset", "0"),
            new("client_req_type", "3"),
            new("category_id", "5000"),
            new("selected_item_from_front_page", "5000"),
            new("is_merged_landing_page", "true"),
            new("category_new_page_715", "1"),
        ];

        AppResponse<AppTagLandingData> response = await _appTransport
            .GetAsync<AppTagLandingData>(
                "/reading/bookapi/new_category/landing/v/",
                query,
                cancellationToken
            )
            .ConfigureAwait(false);

        AppTagLandingData data = response.Data ?? new();
        Dictionary<string, List<TagCategoryItem>> mergedGroups = new(StringComparer.Ordinal);
        IReadOnlyList<AppTagSelectorRowDto> rows = data.Selector?.Rows ?? [];
        foreach (AppTagSelectorRowDto row in rows)
        {
            if (row.Items is null || row.Items.Count == 0)
            {
                continue;
            }

            string groupName = !string.IsNullOrWhiteSpace(row.RowName)
                ? row.RowName.Trim()
                : "分类";
            if (!mergedGroups.TryGetValue(groupName, out List<TagCategoryItem>? list))
            {
                list = [];
                mergedGroups[groupName] = list;
            }

            foreach (AppTagSelectorItemDto item in row.Items)
            {
                string id = item.SelectorItemId?.Trim() ?? string.Empty;
                string name = item.ShowName?.Trim() ?? string.Empty;
                if (id.Length > 0 && list.All(existing => existing.Id != id))
                {
                    list.Add(new TagCategoryItem { Id = id, Name = name });
                }
            }
        }

        return
        [
            .. mergedGroups
                .Where(static pair => pair.Value.Count > 0)
                .Select(static pair => new TagCategoryGroup
                {
                    Name = pair.Key,
                    Items = pair.Value,
                }),
        ];
    }

    internal Task<TagDramaPage> GetTagDramaPageAsync(
        string tagItemId,
        TagDramaSortOrder sort,
        int offset,
        int count,
        CancellationToken cancellationToken
    ) => GetTagDramaPageAsync(tagItemId, MapTagSortOrder(sort), offset, count, cancellationToken);

    internal async Task<TagDramaPage> GetTagDramaPageAsync(
        string tagItemId,
        string sortType,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        tagItemId = (tagItemId ?? string.Empty).Trim();
        sortType = string.IsNullOrWhiteSpace(sortType) ? "new_sort_hot" : sortType.Trim();
        offset = Math.Max(0, offset);
        count = Math.Clamp(count, 1, 30);

        List<KeyValuePair<string, string>> query =
        [
            new("genre_type", "130"),
            new("query_gender", "1"),
            new("category_type", "5"),
            new("page_version", "2"),
            new("source", "detail"),
            new("limit", count.ToString(CultureInfo.InvariantCulture)),
            new("offset", offset.ToString(CultureInfo.InvariantCulture)),
            new("client_req_type", offset > 0 ? "2" : "3"),
            new("category_id", "0"),
            new("selected_order", sortType),
        ];

        if (tagItemId.Length > 0)
        {
            string cleanTag = tagItemId.StartsWith("tag:", StringComparison.OrdinalIgnoreCase)
                ? tagItemId[4..]
                : tagItemId;
            query.Add(new("selected_items", cleanTag));
        }

        AppResponse<AppTagLandingData> response = await _appTransport
            .GetAsync<AppTagLandingData>(
                "/reading/bookapi/new_category/landing/v/",
                query,
                cancellationToken
            )
            .ConfigureAwait(false);

        AppTagLandingData data = response.Data ?? new();
        IReadOnlyList<AppTagDramaItem> videoList = data.VideoInfo ?? [];
        List<Drama> items = [.. videoList.Select(MapTagDramaItem)];

        return new TagDramaPage
        {
            Items = items,
            NextOffset = data.Offset,
            HasMore = data.HasMore,
            SessionId = data.SessionId,
        };
    }

    private static Drama MapTagDramaItem(AppTagDramaItem item)
    {
        List<string> tags = [];
        foreach (AppSubTitleItem sub in item.SubTitleList)
        {
            string content = sub.Content.Trim();
            if (content.Length > 0)
            {
                tags.Add(content);
            }
        }

        return new Drama
        {
            Id = item.SeriesId.Trim(),
            Title = item.Title.Trim(),
            Description = item.VideoDesc.Trim(),
            CoverUrl = item.Cover.Trim(),
            Score = item.Score.Trim(),
            EpisodeCount = Math.Max(0, item.EpisodeCount),
            Remark = item.SubTitle.Trim(),
            Tags = tags,
        };
    }

    internal async Task<ActorLeaderboardPage> GetActorLeaderboardAsync(
        int offset = 0,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    )
    {
        List<KeyValuePair<string, string>> query =
        [
            new("cell_id", "7470092475068071998"),
            new("tab_type", "26"),
            new("selected_items", "ranklist_celebrity"),
            new("offset", offset.ToString(CultureInfo.InvariantCulture)),
            new("limit", "0"),
            new("client_req_type", "2"),
            new("client_template", "2"),
            new("unlimited_selector_change_type", offset > 0 ? "2" : "1"),
        ];

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            query.Add(new("session_id", sessionId));
        }

        AppResponse<AppShortRankDataContainer> response = await _appTransport
            .GetAsync<AppShortRankDataContainer>(
                "/reading/bookapi/bookmall/cell/change/v1/",
                query,
                cancellationToken
            )
            .ConfigureAwait(false);

        AppShortRankDataContainer data = response.Data ?? new();
        List<ActorLeaderboardItem> items = [];

        int position = offset + 1;
        foreach (AppShortRankCellItemDto cell in data.CellView.CellData)
        {
            foreach (AppShortRankActorDto actor in cell.ActorData ?? [])
            {
                string userId = ValueNormalizer.First(
                    actor.UserInfo?.BaseInfo?.UserId,
                    actor.UserInfo?.UserId
                );
                string actorId = actor.UserInfo?.BaseInfo?.EffectiveActorId ?? string.Empty;
                string name = actor.UserInfo?.BaseInfo?.UserName ?? string.Empty;
                string avatar = actor.UserInfo?.BaseInfo?.UserAvatar ?? string.Empty;

                List<string> subTitles = [];
                foreach (AppShortRankSubtitleDto sub in actor.Subtitles ?? [])
                {
                    string text = (sub.Content ?? string.Empty).Trim();
                    if (text.Length > 0)
                    {
                        subTitles.Add(text);
                    }
                }

                items.Add(
                    new ActorLeaderboardItem
                    {
                        Position = position++,
                        UserId = userId,
                        ActorId = actorId,
                        Name = name,
                        AvatarUrl = avatar,
                        Popularity = actor.RecTextItem?.RecommendText ?? string.Empty,
                        RepresentativeWork = actor.Intro ?? string.Empty,
                        SubTitles = subTitles,
                        RankChangeType = actor.RankChangeData?.ChangeType ?? 0,
                        RankChangeValue = actor.RankChangeData?.ChangeNum ?? 0,
                    }
                );
            }
        }

        return new ActorLeaderboardPage
        {
            Items = items,
            HasMore = data.HasMore,
            NextOffset = data.NextOffset,
            SessionId = data.SessionId,
        };
    }

    internal Task<ShortDramaLeaderboardPage> GetShortDramaLeaderboardAsync(
        ShortDramaLeaderboardType type = ShortDramaLeaderboardType.Recommend,
        ShortDramaLeaderboardChannel channel = ShortDramaLeaderboardChannel.All,
        string? genreTag = null,
        int offset = 0,
        string? sessionId = null,
        IEnumerable<string>? seenIds = null,
        CancellationToken cancellationToken = default
    )
    {
        string boardId = MapLeaderboardBoardId(type);
        string? subItem = !string.IsNullOrWhiteSpace(genreTag)
            ? genreTag.Trim()
            : MapLeaderboardSubItem(channel);

        return GetShortDramaLeaderboardAsync(
            boardId,
            subItem,
            offset,
            sessionId,
            seenIds,
            cancellationToken
        );
    }

    internal async Task<ShortDramaLeaderboardPage> GetShortDramaLeaderboardAsync(
        string content = "ranklist_hot_sc",
        string? board = null,
        int offset = 0,
        string? sessionId = null,
        IEnumerable<string>? seenIds = null,
        CancellationToken cancellationToken = default
    )
    {
        string selectedBoard = NormalizeBoardId(content);
        List<KeyValuePair<string, string>> query =
        [
            new("cell_id", "7470092475068071998"),
            new("tab_type", "26"),
            new("selected_items", selectedBoard),
            new("offset", offset.ToString(CultureInfo.InvariantCulture)),
            new("limit", "0"),
            new("client_req_type", "2"),
            new("client_template", "2"),
            new("unlimited_selector_change_type", offset > 0 ? "2" : "1"),
        ];

        string? normalizedSub = NormalizeSubItemId(board);
        if (!string.IsNullOrWhiteSpace(normalizedSub))
        {
            query.Add(new("sub_selected_items", normalizedSub));
        }

        if (seenIds is not null)
        {
            string joinedSeen = string.Join(
                ",",
                seenIds.Where(id => !string.IsNullOrWhiteSpace(id))
            );
            if (joinedSeen.Length > 0)
            {
                query.Add(new("filter_ids", joinedSeen));
            }
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            query.Add(new("session_id", sessionId));
        }

        AppResponse<AppShortRankDataContainer> response = await _appTransport
            .GetAsync<AppShortRankDataContainer>(
                "/reading/bookapi/bookmall/cell/change/v1/",
                query,
                cancellationToken
            )
            .ConfigureAwait(false);

        AppShortRankDataContainer data = response.Data ?? new();
        List<Drama> items = [];
        HashSet<string> localSeen = new(StringComparer.Ordinal);

        foreach (AppShortRankCellItemDto cell in data.CellView.CellData)
        {
            foreach (DramaDto video in cell.VideoData)
            {
                Drama drama = DramaMapper.FromDto(video, "短剧");
                if (!string.IsNullOrWhiteSpace(drama.Id) && localSeen.Add(drama.Id))
                {
                    items.Add(drama);
                }
            }
        }

        string boardTitle = ResolveBoardTitle(data, selectedBoard, normalizedSub);

        return new ShortDramaLeaderboardPage
        {
            Items = items,
            BoardTitle = boardTitle,
            HasMore = data.HasMore,
            NextOffset = data.NextOffset,
            SessionId = data.SessionId,
        };
    }

    internal async IAsyncEnumerable<Drama> EnumerateTagDramasAsync(
        string tagItemId = "",
        TagDramaSortOrder sort = TagDramaSortOrder.Hot,
        int pageSize = 12,
        int maxPages = 10,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        int offset = 0;
        HashSet<string> seenIds = new(StringComparer.Ordinal);
        int pageCount = 0;

        while (!cancellationToken.IsCancellationRequested && pageCount < maxPages)
        {
            TagDramaPage page = await GetTagDramaPageAsync(
                    tagItemId,
                    sort,
                    offset,
                    pageSize,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (page.Items.Count == 0)
            {
                yield break;
            }

            foreach (Drama drama in page.Items)
            {
                if (!string.IsNullOrWhiteSpace(drama.Id) && seenIds.Add(drama.Id))
                {
                    yield return drama;
                }
            }

            if (!page.HasMore || page.NextOffset <= offset)
            {
                yield break;
            }

            offset = page.NextOffset;
            pageCount++;
        }
    }

    internal async IAsyncEnumerable<Drama> EnumerateShortDramaLeaderboardAsync(
        ShortDramaLeaderboardType type = ShortDramaLeaderboardType.Recommend,
        ShortDramaLeaderboardChannel channel = ShortDramaLeaderboardChannel.All,
        string? genreTag = null,
        int maxPages = 10,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        int offset = 0;
        string? sessionId = null;
        HashSet<string> seenIds = new(StringComparer.Ordinal);
        int pageCount = 0;

        while (!cancellationToken.IsCancellationRequested && pageCount < maxPages)
        {
            ShortDramaLeaderboardPage page = await GetShortDramaLeaderboardAsync(
                    type,
                    channel,
                    genreTag,
                    offset,
                    sessionId,
                    seenIds,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (page.Items.Count == 0)
            {
                yield break;
            }

            foreach (Drama drama in page.Items)
            {
                if (!string.IsNullOrWhiteSpace(drama.Id) && seenIds.Add(drama.Id))
                {
                    yield return drama;
                }
            }

            if (!page.HasMore || page.NextOffset <= offset)
            {
                yield break;
            }

            offset = page.NextOffset;
            sessionId = page.SessionId;
            pageCount++;
        }
    }

    internal async IAsyncEnumerable<ActorLeaderboardItem> EnumerateActorLeaderboardAsync(
        int maxPages = 10,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        int offset = 0;
        string? sessionId = null;
        HashSet<string> seenActorIds = new(StringComparer.Ordinal);
        int pageCount = 0;

        while (!cancellationToken.IsCancellationRequested && pageCount < maxPages)
        {
            ActorLeaderboardPage page = await GetActorLeaderboardAsync(
                    offset,
                    sessionId,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (page.Items.Count == 0)
            {
                yield break;
            }

            foreach (ActorLeaderboardItem actor in page.Items)
            {
                string id = !string.IsNullOrWhiteSpace(actor.ActorId)
                    ? actor.ActorId
                    : actor.UserId;
                if (!string.IsNullOrWhiteSpace(id) && seenActorIds.Add(id))
                {
                    yield return actor;
                }
            }

            if (!page.HasMore || page.NextOffset <= offset)
            {
                yield break;
            }

            offset = page.NextOffset;
            sessionId = page.SessionId;
            pageCount++;
        }
    }

    private readonly record struct CatalogCursor(
        int Offset,
        string SessionId,
        string LastId,
        string PageSignature,
        bool Initialized,
        bool Exhausted,
        DateTimeOffset UpdatedAt
    );

    private static string MapLeaderboardBoardId(ShortDramaLeaderboardType type) =>
        type switch
        {
            ShortDramaLeaderboardType.HotPlay => "ranklist_hot_play_sc",
            ShortDramaLeaderboardType.NewRelease => "ranklist_new_rank_sc",
            ShortDramaLeaderboardType.HotSearch => "ranklist_hot_search_sc",
            _ => "ranklist_hot_sc", // Recommend
        };

    private static string? MapLeaderboardSubItem(ShortDramaLeaderboardChannel channel) =>
        channel switch
        {
            ShortDramaLeaderboardChannel.Female => "gender_female",
            ShortDramaLeaderboardChannel.Male => "gender_male",
            _ => null, // All => null (总榜)
        };

    private static string NormalizeBoardId(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "ranklist_hot_sc";
        }
        string trimmed = content.Trim();
        return trimmed switch
        {
            "ranklist_hot_play_sc" or "hot_play" or "hot-play" => "ranklist_hot_play_sc",
            "ranklist_new_rank_sc" or "new_rank" or "new" => "ranklist_new_rank_sc",
            "ranklist_hot_search_sc" or "hot_search" => "ranklist_hot_search_sc",
            "ranklist_celebrity" or "celebrity" or "actor" => "ranklist_celebrity",
            _ when trimmed.StartsWith("ranklist_", StringComparison.Ordinal) => trimmed,
            _ => "ranklist_hot_sc",
        };
    }

    private static string? NormalizeSubItemId(string? subItem)
    {
        if (string.IsNullOrWhiteSpace(subItem))
        {
            return null;
        }
        string trimmed = subItem.Trim();
        return trimmed switch
        {
            "female" or "gender_female" => "gender_female",
            "male" or "gender_male" => "gender_male",
            "all" or "default" => null,
            _ => trimmed,
        };
    }

    private static string ResolveBoardTitle(
        AppShortRankDataContainer data,
        string selectedBoard,
        string? selectedSub
    )
    {
        string mainTitle = string.Empty;
        string subTitle = string.Empty;

        IReadOnlyList<AppShortRankSelectorItemDto> items =
            data.CellView.CellSelector?.OuterRow?.Items ?? [];

        AppShortRankSelectorItemDto? matchedBoard =
            items.FirstOrDefault(i => i.SelectorItemId == selectedBoard)
            ?? items.FirstOrDefault(i => i.IsSelected);

        if (matchedBoard is not null)
        {
            mainTitle = matchedBoard.ShowName;
            IReadOnlyList<AppShortRankSelectorItemDto> subItems =
                matchedBoard.SubCellSelector?.OuterRow?.Items ?? [];

            AppShortRankSelectorItemDto? matchedSub = null;
            if (!string.IsNullOrWhiteSpace(selectedSub))
            {
                matchedSub = subItems.FirstOrDefault(s => s.SelectorItemId == selectedSub);
            }
            matchedSub ??= subItems.FirstOrDefault(s =>
                s.IsSelected && !string.IsNullOrWhiteSpace(s.SelectorItemId)
            );

            if (
                matchedSub is not null
                && !string.IsNullOrWhiteSpace(matchedSub.ShowName)
                && matchedSub.ShowName != "总榜"
            )
            {
                subTitle = matchedSub.ShowName;
            }
        }

        if (string.IsNullOrWhiteSpace(mainTitle))
        {
            mainTitle = selectedBoard switch
            {
                "ranklist_hot_play_sc" => "热播榜",
                "ranklist_new_rank_sc" => "新剧榜",
                "ranklist_hot_search_sc" => "热搜榜",
                "ranklist_celebrity" => "演员榜",
                _ => "推荐榜",
            };
        }

        if (string.IsNullOrWhiteSpace(subTitle) && !string.IsNullOrWhiteSpace(selectedSub))
        {
            subTitle = selectedSub switch
            {
                "gender_female" => "女频",
                "gender_male" => "男频",
                _ => string.Empty,
            };
        }

        return !string.IsNullOrWhiteSpace(subTitle) ? $"{mainTitle}·{subTitle}" : mainTitle;
    }

    private static string MapTagSortOrder(TagDramaSortOrder sort) =>
        sort switch
        {
            TagDramaSortOrder.Latest => "new_sort_latest",
            _ => "new_sort_hot",
        };
}
