using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Circle;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Contracts.Web.Search;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Search;

using Drama = PhoenixReadApi.Drama;
using User = PhoenixReadApi.User;

/// <summary>负责搜索、联想、季数补齐和推荐分页。</summary>
internal sealed partial class SearchService : PhoenixFeatureService
{
    // SearchAsync only needs a small name lookup to supplement the web result.

    private const int SearchNameLookupCount = 50;

    private const int MaxSuggestionLimit = 20;

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan SuggestionCacheLifetime = TimeSpan.FromMinutes(2);

    private readonly Lock _stateLock = new();

    private readonly Dictionary<string, CachedValue<SearchPage<Drama>>> _searchCache =
    [
        with(StringComparer.Ordinal),
    ];

    private readonly Dictionary<string, CachedValue<List<SearchSuggestion>>> _suggestionCache =
    [
        with(StringComparer.Ordinal),
    ];

    private readonly ConcurrentDictionary<string, Lazy<Task<SearchPage<Drama>>>> _searchPending =
        new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<
        string,
        Lazy<Task<IReadOnlyList<SearchSuggestion>>>
    > _suggestionPending = new(StringComparer.Ordinal);

    internal SearchService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<SearchPage<ComprehensiveSearchItem>> SearchComprehensiveAsync(
        string keyword,
        int offset,
        int count,
        string? searchId,
        string? passback,
        CancellationToken cancellationToken
    )
    {
        ParsedSearchTabData parsed = await FetchSearchTabCoreAsync(
                keyword,
                offset,
                count,
                "11",
                searchId,
                passback,
                cancellationToken
            )
            .ConfigureAwait(false);
        return BuildSearchPage(parsed.Comprehensive, parsed);
    }

    internal async Task<SearchPage<Drama>> SearchDramasAsync(
        string keyword,
        int offset,
        int count,
        string? searchId,
        string? passback,
        CancellationToken cancellationToken
    )
    {
        string query = NormalizeKeyword(keyword);
        int normalizedOffset = Math.Max(0, offset);
        int normalizedCount = Math.Clamp(count, 1, 50);

        if (
            normalizedOffset == 0
            && normalizedCount == 20
            && string.IsNullOrWhiteSpace(searchId)
            && string.IsNullOrWhiteSpace(passback)
        )
        {
            if (
                TryGetCache(_searchCache, query, out SearchPage<Drama>? cached)
                && cached is not null
            )
            {
                return cached;
            }

            Lazy<Task<SearchPage<Drama>>> pending = _searchPending.GetOrAdd(
                query,
                static (value, state) =>
                    new Lazy<Task<SearchPage<Drama>>>(
                        () => state.SearchDramasCoreAsync(value),
                        LazyThreadSafetyMode.ExecutionAndPublication
                    ),
                this
            );

            return await AwaitSearchAsync(query, pending, cancellationToken).ConfigureAwait(false);
        }

        ParsedSearchTabData parsed = await FetchSearchTabCoreAsync(
                query,
                normalizedOffset,
                normalizedCount,
                "29",
                searchId,
                passback,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (parsed.Dramas.Count == 0)
        {
            try
            {
                int pageNumber = normalizedOffset / Math.Max(1, normalizedCount) + 1;
                SearchPage<Drama> webResult = await FetchWebSearchPagedAsync(
                        query,
                        pageNumber,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                if (webResult.Items.Count > 0)
                {
                    return webResult;
                }
            }
            catch (Exception)
            {
                // 网页兜底不可用时保持空结果
            }
        }

        return BuildSearchPage(parsed.Dramas, parsed);
    }

    internal async Task<SearchPage<VideoSearchItem>> SearchVideosAsync(
        string keyword,
        int offset,
        int count,
        string? searchId,
        string? passback,
        CancellationToken cancellationToken
    )
    {
        ParsedSearchTabData parsed = await FetchSearchTabCoreAsync(
                keyword,
                offset,
                count,
                "30",
                searchId,
                passback,
                cancellationToken
            )
            .ConfigureAwait(false);
        return BuildSearchPage(parsed.Videos, parsed);
    }

    internal async Task<SearchPage<Post>> SearchDiscussionsAsync(
        string keyword,
        int offset,
        int count,
        string? searchId,
        string? passback,
        CancellationToken cancellationToken
    )
    {
        ParsedSearchTabData parsed = await FetchSearchTabCoreAsync(
                keyword,
                offset,
                count,
                "31",
                searchId,
                passback,
                cancellationToken
            )
            .ConfigureAwait(false);
        return BuildSearchPage(parsed.Discussions, parsed);
    }

    internal async Task<SearchPage<Novel>> SearchNovelsAsync(
        string keyword,
        int offset,
        int count,
        string? searchId,
        string? passback,
        CancellationToken cancellationToken
    )
    {
        ParsedSearchTabData parsed = await FetchSearchTabCoreAsync(
                keyword,
                offset,
                count,
                "1",
                searchId,
                passback,
                cancellationToken
            )
            .ConfigureAwait(false);
        return BuildSearchPage(parsed.Novels, parsed);
    }

    internal async Task<SearchPage<User>> SearchUsersAsync(
        string keyword,
        int offset,
        int count,
        string? searchId,
        string? passback,
        CancellationToken cancellationToken
    )
    {
        ParsedSearchTabData parsed = await FetchSearchTabCoreAsync(
                keyword,
                offset,
                count,
                "27",
                searchId,
                passback,
                cancellationToken
            )
            .ConfigureAwait(false);
        return BuildSearchPage(parsed.Users, parsed);
    }

    internal async Task<SearchPage<Audiobook>> SearchAudiobooksAsync(
        string keyword,
        int offset,
        int count,
        string? searchId,
        string? passback,
        CancellationToken cancellationToken
    )
    {
        ParsedSearchTabData parsed = await FetchSearchTabCoreAsync(
                keyword,
                offset,
                count,
                "2",
                searchId,
                passback,
                cancellationToken
            )
            .ConfigureAwait(false);
        return BuildSearchPage(parsed.Audiobooks, parsed);
    }

    private async Task<SearchPage<Drama>> AwaitSearchAsync(
        string query,
        Lazy<Task<SearchPage<Drama>>> pending,
        CancellationToken cancellationToken
    )
    {
        try
        {
            SearchPage<Drama> result = await pending
                .Value.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (result.Warning.Length == 0)
            {
                lock (_stateLock)
                {
                    _searchCache[query] = new CachedValue<SearchPage<Drama>>(
                        result,
                        DateTimeOffset.UtcNow.Add(CacheLifetime)
                    );
                }
            }

            return result;
        }
        finally
        {
            _searchPending.TryRemove(
                new KeyValuePair<string, Lazy<Task<SearchPage<Drama>>>>(query, pending)
            );
        }
    }

    internal Task<IReadOnlyList<SearchSuggestion>> GetSuggestionsAsync(
        string keyword,
        int maxResults,
        CancellationToken cancellationToken
    )
    {
        string query = NormalizeKeyword(keyword);

        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "搜索建议数量必须大于 0。");
        }

        int limit = Math.Min(maxResults, MaxSuggestionLimit);
        string cacheKey = SuggestionCacheKey(query, limit);

        if (
            TryGetCache(_suggestionCache, cacheKey, out List<SearchSuggestion>? cached)
            && cached is not null
        )
        {
            return Task.FromResult<IReadOnlyList<SearchSuggestion>>(cached);
        }

        (SearchService Service, string Query, int Limit) state = (
            Service: this,
            Query: query,
            Limit: limit
        );

        Lazy<Task<IReadOnlyList<SearchSuggestion>>> pending = _suggestionPending.GetOrAdd(
            cacheKey,
            static (_, state) =>
                new Lazy<Task<IReadOnlyList<SearchSuggestion>>>(
                    () => state.Service.LoadSuggestionsAsync(state.Query, state.Limit),
                    LazyThreadSafetyMode.ExecutionAndPublication
                ),
            state
        );

        return AwaitSuggestionsAsync(cacheKey, pending, cancellationToken);
    }

    private async Task<IReadOnlyList<SearchSuggestion>> AwaitSuggestionsAsync(
        string query,
        Lazy<Task<IReadOnlyList<SearchSuggestion>>> pending,
        CancellationToken cancellationToken
    )
    {
        try
        {
            IReadOnlyList<SearchSuggestion> result = await pending
                .Value.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            lock (_stateLock)
            {
                _suggestionCache[query] = new CachedValue<List<SearchSuggestion>>(
                    [.. result],
                    DateTimeOffset.UtcNow.Add(SuggestionCacheLifetime)
                );
            }

            return result;
        }
        finally
        {
            _suggestionPending.TryRemove(
                new KeyValuePair<string, Lazy<Task<IReadOnlyList<SearchSuggestion>>>>(
                    query,
                    pending
                )
            );
        }
    }

    internal async Task<IReadOnlyList<string>> GetSearchCuesAsync(
        CancellationToken cancellationToken
    )
    {
        KeyValuePair<string, string>[] query =
        [
            new("cue_type", "1"),
            new("limit", "20"),
            new("search_source", "1"),
            new("tab_name", "feed"),
            new("user_is_login", UserIsLogin),
        ];

        AppSearchCueResponse response = await _appTransport
            .GetRootAsync<AppSearchCueResponse>(
                "/reading/bookapi/search/cue/v1/",
                query,
                cancellationToken
            )
            .ConfigureAwait(false);

        List<string> result = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];

        foreach (AppSearchCueItemDto item in response.DataV1)
        {
            string text = item.Text.Trim();

            if (text.Length > 0 && seen.Add(text))
            {
                result.Add(text);
            }
        }

        if (result.Count == 0 && !string.IsNullOrWhiteSpace(response.Data.Text))
        {
            result.Add(response.Data.Text.Trim());
        }

        return result;
    }

    internal async Task<HotSearch> GetHotSearchAsync(CancellationToken cancellationToken)
    {
        KeyValuePair<string, string>[] query =
        [
            new("user_is_login", UserIsLogin),
            new("bookstore_tab", "16"),
            new("bookstore_tab_type", "16"),
            new("scene", "10"),
            new("limit", "0"),
            new("tab_name", "feed"),
            new("from", "search_input_page"),
            new("query_history_removed", "false"),
            new("offset", "0"),
            new("search_middle_page_version", "4"),
            new("search_source", "1"),
            new("new_search_middle_page", "true"),
            new("hot_word_exchange", "false"),
        ];

        AppResponse<IReadOnlyList<AppHotSearchPlanCellDto>> response = await _appTransport
            .GetAsync<IReadOnlyList<AppHotSearchPlanCellDto>>(
                "/reading/bookapi/plan/v1/",
                query,
                cancellationToken
            )
            .ConfigureAwait(false);

        List<HotSearchItem> guesses = [];
        List<HotSearchBoard> boards = [];
        IReadOnlyList<AppHotSearchPlanCellDto> cells = response.Data ?? [];

        foreach (AppHotSearchPlanCellDto planCell in cells)
        {
            if (string.IsNullOrWhiteSpace(planCell.LynxData))
            {
                continue;
            }

            List<AppHotSearchLynxCellDto>? lynxCells = null;

            try
            {
                lynxCells = JsonAotSerializer.Deserialize<List<AppHotSearchLynxCellDto>>(
                    planCell.LynxData
                );
            }
            catch (JsonException)
            {
                continue;
            }

            if (lynxCells is null)
            {
                continue;
            }

            foreach (AppHotSearchLynxCellDto cell in lynxCells)
            {
                if (cell.CellName == "猜你想搜" || cell.ShowType == 483)
                {
                    foreach (AppHotSearchVideoDto video in cell.VideoData ?? [])
                    {
                        guesses.Add(
                            new HotSearchItem
                            {
                                Id = video.SeriesId ?? string.Empty,
                                Title = video.Title ?? string.Empty,
                                CoverUrl = video.Cover ?? string.Empty,
                                EpisodeCount = video.EpisodeCount,
                                CardTips = video.CardTips ?? string.Empty,
                                Heat = video.RecText ?? string.Empty,
                                Tag = video.TagInfo?.Text ?? string.Empty,
                            }
                        );
                    }
                }
                else if (cell.CellData.Count > 0)
                {
                    foreach (AppHotSearchLynxCellDto boardCell in cell.CellData)
                    {
                        string boardTitle = !string.IsNullOrWhiteSpace(boardCell.CellName)
                            ? boardCell.CellName
                            : boardCell.ShowType switch
                            {
                                388 => "短剧热搜榜",
                                591 => "漫剧热搜榜",
                                619 => "热点话题榜",
                                _ => "热搜榜",
                            };

                        List<HotSearchItem> boardItems = [];

                        foreach (AppHotSearchVideoDto video in boardCell.VideoData ?? [])
                        {
                            boardItems.Add(
                                new HotSearchItem
                                {
                                    Id = video.SeriesId ?? string.Empty,
                                    Title = video.Title ?? string.Empty,
                                    CoverUrl = video.Cover ?? string.Empty,
                                    EpisodeCount = video.EpisodeCount,
                                    CardTips = video.CardTips ?? string.Empty,
                                    Heat = video.RecText ?? string.Empty,
                                    Tag = video.TagInfo?.Text ?? string.Empty,
                                }
                            );
                        }

                        foreach (AppHotSearchTopicDto topic in boardCell.VideoTopics ?? [])
                        {
                            string heatInfo =
                                topic.SecondaryInfoList is { Count: > 0 } sec && sec[0] is not null
                                    ? sec[0].Content ?? string.Empty
                                    : string.Empty;

                            boardItems.Add(
                                new HotSearchItem
                                {
                                    Id = topic.TopicId ?? string.Empty,
                                    Title = topic.Title ?? string.Empty,
                                    Url = topic.Url ?? string.Empty,
                                    Heat = heatInfo,
                                }
                            );
                        }

                        if (boardItems.Count > 0)
                        {
                            boards.Add(
                                new HotSearchBoard { Title = boardTitle, Items = boardItems }
                            );
                        }
                    }
                }
            }
        }

        return new HotSearch { Guesses = guesses, Boards = boards };
    }

    private async Task<SearchPage<Drama>> SearchDramasCoreAsync(string keyword)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(40));

        CancellationToken cancellationToken = timeout.Token;

        using CancellationTokenSource pageTimeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );
        pageTimeout.CancelAfter(TimeSpan.FromSeconds(12));

        Task<List<Drama>> namesTask = FetchSearchNamesAsync(keyword, cancellationToken);
        Task<SearchPage<Drama>> pageTask = FetchSearchDramaPageAsync(keyword, pageTimeout.Token);

        List<Drama> names = [];
        Exception? namesError = null;
        try
        {
            names = await namesTask.ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            namesError = exception;
        }

        SearchPage<Drama> page = new();
        Exception? pageError = null;
        try
        {
            page = await pageTask.ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            pageError = exception;
        }

        if (cancellationToken.IsCancellationRequested && names.Count == 0 && page.Items.Count == 0)
        {
            throw new TimeoutException("搜索已到时限。");
        }

        List<Drama> merged = MergeDramas([.. page.Items], names);
        bool limited = page.Limited || pageError is not null || namesError is not null;
        string warning = string.Empty;

        if (pageError is not null)
        {
            warning = "红果综合搜索暂不可用，已保留名称匹配结果，可重试";
        }
        else if (namesError is not null)
        {
            warning = "红果名称检索暂不可用，结果可能缺少部分剧集，可重试";
        }

        if (
            _options.CompleteSearchSeasons
            && merged.Count > 0
            && !cancellationToken.IsCancellationRequested
        )
        {
            bool seasonLimited = await CompleteSearchSeasonsAsync(
                    keyword,
                    merged,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (seasonLimited)
            {
                warning =
                    warning.Length == 0
                        ? "部分季数暂未补齐，已保留当前结果，可重试"
                        : warning + "；部分季数暂未补齐，已保留当前结果，可重试";

                limited = true;
            }
        }

        merged.Sort((left, right) => CompareSearchDrama(left, right, keyword));

        return new SearchPage<Drama>
        {
            Items = merged,
            Total = Math.Max(page.Total, merged.Count),
            NextOffset =
                page.NextOffset > 0 ? page.NextOffset
                : page.HasMore ? 20
                : 0,
            HasMore = page.HasMore,
            SearchId = page.SearchId,
            Limited = limited,
            Warning = warning,
        };
    }

    private async Task<List<Drama>> FetchSearchNamesAsync(
        string keyword,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<WebSuggestionDto> records = await FetchSuggestionRecordsAsync(
                keyword,
                SearchNameLookupCount,
                cancellationToken
            )
            .ConfigureAwait(false);

        List<Drama> output = [];

        foreach (WebSuggestionDto record in records)
        {
            if (!string.Equals(record.WordType, "short_play_name", StringComparison.Ordinal))
            {
                continue;
            }

            DramaDto data = record.VideoData;

            if (
                data is null
                || !ValueNormalizer.IsNumericId(
                    ValueNormalizer.First(data.SeriesId, data.SeriesIdNumber)
                )
            )
            {
                continue;
            }

            Drama drama = DramaMapper.FromDto(data, "短剧");

            if (drama.Title == drama.Id && !string.IsNullOrWhiteSpace(record.Name))
            {
                string title = record.Name.Trim();

                drama = new Drama
                {
                    Id = drama.Id,
                    Title = title,
                    Description = drama.Description,
                    CoverUrl = drama.CoverUrl,
                    Category = drama.Category,
                    Remark = drama.Remark,
                    Score = drama.Score,
                    Views = drama.Views,
                    Heat = drama.Heat,
                    OnlineDate = drama.OnlineDate,
                    EpisodeCount = drama.EpisodeCount,
                    ReleaseStatus = drama.ReleaseStatus,
                    Tags = drama.Tags,
                };
            }

            if (drama.Id.Length > 0 && drama.Title != drama.Id)
            {
                output.Add(drama);
            }
        }

        return MergeDramas([], output);
    }

    private async Task<SearchPage<Drama>> FetchSearchDramaPageAsync(
        string keyword,
        CancellationToken cancellationToken
    )
    {
        try
        {
            ParsedSearchTabData parsed = await FetchSearchTabCoreAsync(
                    keyword,
                    offset: 0,
                    count: 20,
                    tabType: "29",
                    searchId: null,
                    passback: null,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (parsed.Dramas.Count > 0)
            {
                return BuildSearchPage(parsed.Dramas, parsed);
            }
        }
        catch (Exception)
        {
            // App 搜索接口不可用时回退到网页搜索
        }

        return await FetchWebSearchPagedAsync(keyword, 1, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SearchPage<Drama>> FetchWebSearchPagedAsync(
        string keyword,
        int pageNumber,
        CancellationToken cancellationToken
    )
    {
        Uri uri = new(
            _options.WebBaseUri,
            $"api/search/result?aid=8662&query={Uri.EscapeDataString(keyword)}&page={pageNumber}"
        );

        string body = await GetTextAsync(uri, _options.WebBaseUri, "search", cancellationToken)
            .ConfigureAwait(false);

        WebSearchPageDto? page;

        try
        {
            page = JsonAotSerializer.Deserialize<WebSearchPageDto>(body);
        }
        catch (JsonException exception)
        {
            throw new PhoenixReadApiResponseException(
                "红果搜索未返回有效结果。",
                "search",
                exception
            );
        }

        if (page is null || page.Items.Count == 0)
        {
            throw new PhoenixReadApiResponseException("红果搜索未返回有效结果。", "search");
        }

        List<Drama> output = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];

        foreach (WebSearchRecordDto row in page.Items)
        {
            if (row.VideoData is not null)
            {
                Drama drama = DramaMapper.FromDto(row.VideoData, "短剧");

                if (drama.Id.Length > 0 && seen.Add(drama.Id))
                {
                    output.Add(drama);
                }
            }
        }

        if (page.Items.Count > 0 && output.Count == 0)
        {
            throw new PhoenixReadApiResponseException("搜索结果中没有可识别的剧集。", "search");
        }

        return new SearchPage<Drama>
        {
            Items = output,
            Total = Math.Max(page.TotalCount, output.Count),
            NextOffset = page.HasMore ? pageNumber * 20 : 0,
            HasMore = page.HasMore,
            Limited = page.HasMore,
        };
    }

    private async Task<ParsedSearchTabData> FetchSearchTabCoreAsync(
        string keyword,
        int offset,
        int count,
        string tabType,
        string? searchId,
        string? passback,
        CancellationToken cancellationToken
    )
    {
        keyword = NormalizeKeyword(keyword);
        offset = Math.Max(0, offset);
        count = Math.Clamp(count, 1, 50);

        List<KeyValuePair<string, string>> query =
        [
            new("query", keyword),
            new("offset", offset.ToString(CultureInfo.InvariantCulture)),
            new("count", count.ToString(CultureInfo.InvariantCulture)),
            new("tab_type", tabType),
            new("use_correct", "true"),
            new("bookshelf_search_plan", "4"),
            new("search_source", "1"),
            new("tab_name", "feed"),
            new("bookstore_tab", "16"),
            new("user_is_login", UserIsLogin),
        ];

        if (!string.IsNullOrWhiteSpace(searchId))
        {
            query.Add(new("search_id", searchId));
        }

        if (!string.IsNullOrWhiteSpace(passback))
        {
            query.Add(new("passback", passback));
        }
        else if (offset > 0)
        {
            query.Add(new("passback", offset.ToString(CultureInfo.InvariantCulture)));
        }

        AppSearchTabResponse? tabResponse = null;

        try
        {
            tabResponse = await _appTransport
                .GetSearchTabAsync("/reading/bookapi/search/tab/v/", query, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            // App 搜索接口不可用时允许降级
        }

        IReadOnlyList<AppSearchTabItem> allTabs = (
            (tabResponse?.SearchTabs?.Count ?? 0) > 0
                ? tabResponse!.SearchTabs
                : tabResponse?.Data?.SearchTabs
        ) ?? [];

        AppSearchTabItem? tab = allTabs.FirstOrDefault(t =>
            t.TabType == tabType && t.Data is { Count: > 0 }
        ) ?? allTabs.FirstOrDefault(static t => t.Data is { Count: > 0 });

        ParsedSearchTabData parsed = ParseSearchTabCells(tab?.Data ?? [], tabType);
        parsed.Offset = offset;
        parsed.NextOffset = tab?.NextOffset ?? 0;
        parsed.HasMore = tab?.HasMore ?? false;
        parsed.SearchId = tab?.SearchId ?? string.Empty;
        parsed.Passback = tab?.Passback ?? string.Empty;
        return parsed;
    }

    private static SearchPage<T> BuildSearchPage<T>(List<T> items, ParsedSearchTabData parsed)
    {
        int nextOffset =
            parsed.NextOffset > 0
                ? parsed.NextOffset
                : (items.Count > 0 ? parsed.Offset + items.Count : 0);

        return new SearchPage<T>
        {
            Items = items,
            Total = items.Count,
            NextOffset = nextOffset,
            HasMore = parsed.HasMore,
            SearchId = parsed.SearchId,
            Passback = parsed.Passback,
            Limited = parsed.HasMore || items.Count == 0,
            Warning =
                items.Count == 0
                    ? "红果服务端当前未返回该分类搜索结果（可能受到匿名频控限制）。"
                    : string.Empty,
        };
    }

    private async Task<IReadOnlyList<SearchSuggestion>> LoadSuggestionsAsync(
        string query,
        int maxResults
    )
    {
        IReadOnlyList<WebSuggestionDto> records = await FetchSuggestionRecordsAsync(
                query,
                maxResults,
                CancellationToken.None
            )
            .ConfigureAwait(false);

        List<SearchSuggestion> output = [with(maxResults)];
        HashSet<string> seen = [with(StringComparer.OrdinalIgnoreCase)];

        foreach (WebSuggestionDto record in records)
        {
            string name = record.Name.Trim();

            if (name.Length is 0 or > 80 || name.Any(char.IsControl) || !seen.Add(name))
            {
                continue;
            }

            SearchSuggestionType type = record.WordType switch
            {
                "short_play_name" => SearchSuggestionType.ShortPlayName,
                "short_play_category" => SearchSuggestionType.ShortPlayCategory,
                "common_query" => SearchSuggestionType.CommonQuery,
                "actor_name" => SearchSuggestionType.ActorName,
                "short_play_actor" => SearchSuggestionType.ShortPlayActor,
                _ => SearchSuggestionType.Unknown,
            };

            output.Add(new SearchSuggestion { Name = name, Type = type });

            if (output.Count == maxResults)
            {
                break;
            }
        }

        return output;
    }

    private static string SuggestionCacheKey(string query, int maxResults)
    {
        return query + "\0" + maxResults.ToString(CultureInfo.InvariantCulture);
    }

    private async Task<IReadOnlyList<WebSuggestionDto>> FetchSuggestionRecordsAsync(
        string query,
        int count,
        CancellationToken cancellationToken
    )
    {
        Uri uri = new(
            _options.WebBaseUri,
            $"incent_resource/suggestion?app_id=8662&query={Uri.EscapeDataString(query)}&count={count.ToString(CultureInfo.InvariantCulture)}"
        );

        string body = await GetTextAsync(
                uri,
                _options.WebBaseUri,
                "suggestions",
                cancellationToken,
                256 * 1024
            )
            .ConfigureAwait(false);

        try
        {
            WebSuggestionResponseDto? data =
                JsonAotSerializer.Deserialize<WebSuggestionResponseDto>(body);

            IReadOnlyList<WebSuggestionDto>? items = data?.SuggestList;

            return items
                ?? throw new PhoenixReadApiResponseException(
                    "红果搜索联想未返回有效数据。",
                    "suggestions"
                );
        }
        catch (JsonException exception)
        {
            throw new PhoenixReadApiResponseException(
                "红果搜索联想未返回有效数据。",
                "suggestions",
                exception
            );
        }
    }

    private async Task<bool> CompleteSearchSeasonsAsync(
        string keyword,
        List<Drama> dramas,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, SeasonGroup> groups = [with(StringComparer.Ordinal)];

        foreach (Drama drama in dramas)
        {
            if (!TryParseSeason(drama.Title, out string baseTitle, out int season, out string unit))
            {
                continue;
            }

            string key = SearchText(baseTitle) + ":" + unit;

            if (!groups.TryGetValue(key, out SeasonGroup? group))
            {
                group = new SeasonGroup(baseTitle, SearchText(baseTitle), unit);
                groups.Add(key, group);
            }

            group.Known.Add(season);
            group.Maximum = Math.Max(group.Maximum, season);
        }

        SeasonGroup[] incomplete =
        [
            .. groups
                .Values.Where(static group =>
                    group.Known.Count >= 2 && group.Known.Count < group.Maximum
                )
                .OrderBy(static group => group.Title, StringComparer.Ordinal),
        ];

        if (incomplete.Length == 0)
        {
            return false;
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );

        timeout.CancelAfter(TimeSpan.FromSeconds(25));

        HashSet<string> attempted = [with(StringComparer.OrdinalIgnoreCase), keyword];

        int requestCount = 0;

        foreach (SeasonGroup group in incomplete)
        {
            while (requestCount < 32 && !timeout.IsCancellationRequested)
            {
                int missing = 0;
                for (int number = 1; number <= group.Maximum; number++)
                {
                    if (!group.Known.Contains(number))
                    {
                        missing = number;
                        break;
                    }
                }

                if (missing == 0)
                {
                    break;
                }

                string[] labels =
                [
                    ChineseSeason(missing),
                    missing.ToString(CultureInfo.InvariantCulture),
                ];

                string[] queries =
                    missing == 1
                        ?
                        [
                            group.Title,
                            group.Title + "第" + labels[0] + group.Unit,
                            group.Title + "第1" + group.Unit,
                        ]
                        :
                        [
                            group.Title + "第" + labels[0] + group.Unit,
                            group.Title + "第" + labels[1] + group.Unit,
                        ];

                string? query = queries.FirstOrDefault(attempted.Add);

                if (query is null)
                {
                    break;
                }

                requestCount++;

                List<Drama> matches;

                try
                {
                    matches = await FetchSearchNamesAsync(query, timeout.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return true;
                }
                catch (PhoenixReadApiException)
                {
                    return true;
                }

                foreach (Drama match in matches)
                {
                    if (
                        !TryParseSeason(
                            match.Title,
                            out string baseTitle,
                            out int season,
                            out string unit
                        )
                        || unit != group.Unit
                        || SearchText(baseTitle) != group.Key
                    )
                    {
                        continue;
                    }

                    group.Known.Add(season);
                }

                List<Drama> merged = MergeDramas(dramas, matches);

                dramas.Clear();

                dramas.AddRange(merged);
            }
        }

        return incomplete.Any(group => group.Known.Count < group.Maximum);
    }

    private static int CompareSearchDrama(Drama left, Drama right, string query)
    {
        int leftRank = TitleRank(left.Title, query);

        int rightRank = TitleRank(right.Title, query);

        if (leftRank != rightRank)
        {
            return leftRank.CompareTo(rightRank);
        }

        if (
            TryParseSeason(left.Title, out string leftBase, out int leftSeason, out string leftUnit)
            && TryParseSeason(
                right.Title,
                out string rightBase,
                out int rightSeason,
                out string rightUnit
            )
            && leftUnit == rightUnit
            && SearchText(leftBase) == SearchText(rightBase)
        )
        {
            return leftSeason.CompareTo(rightSeason);
        }

        return string.Compare(left.Title, right.Title, StringComparison.Ordinal);
    }

    private static bool TryParseSeason(
        string title,
        out string baseTitle,
        out int season,
        out string unit
    )
    {
        Match match = SeasonRegex().Match(title.Normalize(NormalizationForm.FormKC).Trim());

        baseTitle = string.Empty;

        season = 0;

        unit = string.Empty;

        if (
            !match.Success
            || !int.TryParse(
                match.Groups[2].Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out season
            )
        )
        {
            string label = match.Success ? match.Groups[2].Value : string.Empty;

            season = ParseChineseSeason(label);
        }

        if (!match.Success || season is < 1 or > 200)
        {
            return false;
        }

        baseTitle = title[..match.Index].TrimEnd().TrimEnd("，。！？:：、·-— ".ToCharArray());

        unit = match.Groups[3].Value;

        return baseTitle.Length > 0;
    }

    private static int ParseChineseSeason(string value)
    {
        if (value.Length == 0)
        {
            return 0;
        }

        Dictionary<char, int> digits = new()
        {
            ['零'] = 0,
            ['〇'] = 0,
            ['一'] = 1,
            ['二'] = 2,
            ['两'] = 2,
            ['兩'] = 2,
            ['三'] = 3,
            ['四'] = 4,
            ['五'] = 5,
            ['六'] = 6,
            ['七'] = 7,
            ['八'] = 8,
            ['九'] = 9,
        };

        if (!value.Any(static c => c is '十' or '百'))
        {
            int number = 0;

            foreach (char c in value)
            {
                if (!digits.TryGetValue(c, out int digit))
                {
                    return 0;
                }

                number = number * 10 + digit;
            }

            return number;
        }

        int result = 0;
        int current = 0;
        int previousUnit = 1000;

        foreach (char c in value)
        {
            if (digits.TryGetValue(c, out int digit))
            {
                current = current * 10 + digit;
                continue;
            }

            int unit = c == '百' ? 100 : 10;

            if (unit >= previousUnit || current > 9)
            {
                return 0;
            }

            result += Math.Max(current, 1) * unit;
            current = 0;
            previousUnit = unit;
        }

        return result + current;
    }

    private static string ChineseSeason(int number)
    {
        const string digits = "零一二三四五六七八九";

        if (number < 10)
        {
            return digits[number].ToString();
        }

        if (number < 100)
        {
            string prefix = number >= 20 ? digits[number / 10].ToString() : string.Empty;

            return prefix
                + "十"
                + (number % 10 == 0 ? string.Empty : digits[number % 10].ToString());
        }

        string hundreds = digits[number / 100] + "百";

        int rest = number % 100;

        if (rest == 0)
        {
            return hundreds;
        }

        return hundreds
            + (
                rest < 10 ? "零"
                : rest < 20 ? "一"
                : string.Empty
            )
            + ChineseSeason(rest);
    }

    private static string SearchText(string value)
    {
        StringBuilder builder = new();

        foreach (Rune rune in value.Normalize(NormalizationForm.FormKC).EnumerateRunes())
        {
            if (
                Rune.GetUnicodeCategory(rune)
                is UnicodeCategory.SpaceSeparator
                    or UnicodeCategory.ConnectorPunctuation
                    or UnicodeCategory.DashPunctuation
                    or UnicodeCategory.OpenPunctuation
                    or UnicodeCategory.ClosePunctuation
                    or UnicodeCategory.InitialQuotePunctuation
                    or UnicodeCategory.FinalQuotePunctuation
                    or UnicodeCategory.OtherPunctuation
            )
            {
                continue;
            }

            builder.Append(rune.ToString().ToLowerInvariant());
        }

        return builder.Length == 0 ? value.Trim().ToLowerInvariant() : builder.ToString();
    }

    private static int TitleRank(string title, string query)
    {
        string normalizedTitle = SearchText(title);

        string normalizedQuery = SearchText(query);

        if (normalizedTitle == normalizedQuery)
        {
            return 0;
        }

        if (normalizedTitle.StartsWith(normalizedQuery, StringComparison.Ordinal))
        {
            return 1;
        }

        if (normalizedTitle.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return 2;
        }

        string[] words =
        [
            .. query
                .Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
                .Select(SearchText),
        ];

        return words.Length > 0 && words.All(normalizedTitle.Contains) ? 3 : 4;
    }

    private bool TryGetCache<T>(Dictionary<string, CachedValue<T>> cache, string key, out T? value)
    {
        lock (_stateLock)
        {
            if (
                cache.TryGetValue(key, out CachedValue<T>? cached)
                && cached.ExpiresAt > DateTimeOffset.UtcNow
            )
            {
                value = cached.Value;

                return true;
            }

            cache.Remove(key);
        }

        value = default;

        return false;
    }

    private static ParsedSearchTabData ParseSearchTabCells(
        IReadOnlyList<AppSearchTabCell> cells,
        string tabType
    )
    {
        ParsedSearchTabData result = new();
        HashSet<string> seenDramas = [with(StringComparer.Ordinal)];
        HashSet<string> seenAlbums = [with(StringComparer.Ordinal)];
        HashSet<string> seenShortVideos = [with(StringComparer.Ordinal)];
        HashSet<string> seenDiscussions = [with(StringComparer.Ordinal)];
        HashSet<string> seenNovels = [with(StringComparer.Ordinal)];
        HashSet<string> seenAudiobooks = [with(StringComparer.Ordinal)];
        HashSet<string> seenUsers = [with(StringComparer.Ordinal)];

        foreach (AppSearchTabCell cell in cells)
        {
            if (cell is null)
            {
                continue;
            }

            foreach (DramaDto video in cell.VideoData ?? [])
            {
                if (video is null)
                {
                    continue;
                }

                if (video.AlbumInfo is not null)
                {
                    string albumId = video.AlbumInfo.EffectiveAlbumId;
                    if (albumId.Length > 0 && seenAlbums.Add(albumId))
                    {
                        DramaAlbum album = new()
                        {
                            Id = albumId,
                            Title = ValueNormalizer.First(
                                video.AlbumInfo.Title,
                                video.Title,
                                string.Empty
                            ),
                            Description = ValueNormalizer.First(
                                video.AlbumInfo.Intro,
                                video.Intro,
                                string.Empty
                            ),
                            CoverUrl = ValueNormalizer.First(
                                video.AlbumInfo.Cover,
                                video.SeriesCover,
                                string.Empty
                            ),
                            SeasonCount = Math.Max(0, video.AlbumInfo.EpisodeCount),
                        };
                        result.Albums.Add(album);
                        result.Videos.Add(album);
                        result.Comprehensive.Add(album);
                    }
                    continue;
                }

                if (video.ContentType == 1001)
                {
                    string vid = ValueNormalizer.First(
                        video.SeriesId,
                        video.SeriesIdNumber,
                        string.Empty
                    );
                    if (vid.Length > 0 && seenShortVideos.Add(vid))
                    {
                        ShortVideo shortVideo = new()
                        {
                            Id = vid,
                            VideoId = vid,
                            Title = ValueNormalizer.First(
                                video.SeriesTitle,
                                video.Title,
                                video.SeriesName,
                                string.Empty
                            ),
                            Description = ValueNormalizer.First(
                                video.SeriesIntro,
                                video.VideoDescription,
                                video.Intro,
                                string.Empty
                            ),
                            CoverUrl = ValueNormalizer.First(video.SeriesCover, string.Empty),
                            DurationSeconds = Math.Max(0, video.DurationSeconds),
                        };
                        result.ShortVideos.Add(shortVideo);
                        result.Videos.Add(shortVideo);
                        result.Comprehensive.Add(shortVideo);
                    }
                    continue;
                }

                Drama drama = DramaMapper.FromDto(video, "短剧");
                if (drama.Id.Length > 0 && seenDramas.Add(drama.Id))
                {
                    result.Dramas.Add(drama);
                    result.Comprehensive.Add(drama);
                }
            }

            foreach (AppSearchPostDto post in cell.PostData ?? [])
            {
                if (post is null || string.IsNullOrWhiteSpace(post.PostId))
                {
                    continue;
                }

                if (seenDiscussions.Add(post.PostId))
                {
                    Post mapped = MapSearchPost(post);
                    result.Discussions.Add(mapped);
                    result.Comprehensive.Add(mapped);
                }
            }

            foreach (AppSearchBookDto book in cell.BookData ?? [])
            {
                if (
                    book is null
                    || string.IsNullOrWhiteSpace(book.BookId)
                    || string.IsNullOrWhiteSpace(book.BookName)
                )
                {
                    continue;
                }

                bool isAudio =
                    tabType == "2"
                    || book.Genre == "4"
                    || !string.IsNullOrWhiteSpace(book.AudioThumbUri);

                if (isAudio)
                {
                    if (seenAudiobooks.Add(book.BookId))
                    {
                        Audiobook audiobook = new()
                        {
                            Id = book.BookId,
                            Title = book.BookName,
                            Author = book.Author,
                            Description = book.Abstract,
                            Category = book.Category,
                            Status = MapNovelStatus(book.CreationStatus),
                            StatusText = book.CreationStatus,
                            CoverUrl = ValueNormalizer.First(
                                book.AudioThumbUrlHd,
                                book.AudioThumbUri,
                                book.CoverUrl,
                                string.Empty
                            ),
                            ListenCount = Math.Max(book.ListenCount, book.ReadCount),
                            Score = book.Score,
                            SerialCount = book.SerialCount,
                            Tags =
                            [
                                .. book
                                    .Tags.Select(static t => t.Name)
                                    .Where(static s => !string.IsNullOrWhiteSpace(s)),
                            ],
                        };
                        result.Audiobooks.Add(audiobook);
                        result.Comprehensive.Add(audiobook);
                    }

                    if (tabType == "2")
                    {
                        continue;
                    }
                }

                if (seenNovels.Add(book.BookId))
                {
                    Novel novel = new()
                    {
                        Id = book.BookId,
                        Title = book.BookName,
                        ShortTitle = book.BookShortName,
                        Author = book.Author,
                        Description = book.Abstract,
                        Status = MapNovelStatus(book.CreationStatus),
                        StatusText = book.CreationStatus,
                        CoverUrl = book.CoverUrl,
                        SerialCount = book.SerialCount,
                        ReadCount = book.ReadCount,
                        Tags =
                        [
                            .. book
                                .Tags.Select(static t => t.Name)
                                .Where(static s => !string.IsNullOrWhiteSpace(s)),
                        ],
                    };
                    result.Novels.Add(novel);
                    if (!isAudio)
                    {
                        result.Comprehensive.Add(novel);
                    }
                }
            }

            foreach (AppSearchUserDto userDto in cell.CelebrityUserList ?? [])
            {
                if (userDto is null || string.IsNullOrWhiteSpace(userDto.UserId))
                {
                    continue;
                }

                string userName = userDto.BaseInfo?.UserName ?? string.Empty;

                if (string.IsNullOrWhiteSpace(userName))
                {
                    continue;
                }

                if (seenUsers.Add(userDto.UserId))
                {
                    User user = new()
                    {
                        Id = userDto.UserId,
                        Name = userName,
                        Introduction = userDto.BaseInfo?.Description ?? string.Empty,
                        AvatarUrl = userDto.BaseInfo?.EffectiveAvatarUrl ?? string.Empty,
                    };
                    result.Users.Add(user);
                    result.Comprehensive.Add(user);
                }
            }
        }

        return result;
    }

    internal static Post MapSearchPost(AppSearchPostDto post)
    {
        List<CircleImage> images = [];
        List<string> topics =
        [
            .. post
                .Tags.Select(static t => t.Name)
                .Where(static s => !string.IsNullOrWhiteSpace(s)),
        ];

        if (!string.IsNullOrWhiteSpace(post.Content))
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(post.Content);
                AppCirclePostMaterialContainer? container =
                    JsonAotSerializer.Deserialize<AppCirclePostMaterialContainer>(bytes);
                if (container?.Materials is not null)
                {
                    foreach (AppCirclePostMaterial material in container.Materials)
                    {
                        if (
                            string.Equals(
                                material.Type,
                                "img-list",
                                StringComparison.OrdinalIgnoreCase
                            ) && material.Data?.Images is not null
                        )
                        {
                            foreach (AppCirclePostImageItem img in material.Data.Images)
                            {
                                if (!string.IsNullOrWhiteSpace(img.WebUrl))
                                {
                                    images.Add(
                                        new CircleImage
                                        {
                                            Url = img.WebUrl,
                                            Width = img.Width,
                                            Height = img.Height,
                                        }
                                    );
                                }
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // 忽略非标准富文本格式
            }
        }

        if (images.Count == 0 && !string.IsNullOrWhiteSpace(post.CoverUrl))
        {
            images.Add(new CircleImage { Url = post.CoverUrl });
        }

        List<Drama> relatedDramas = [];
        foreach (DramaDto card in post.BookCards)
        {
            Drama related = DramaMapper.FromDto(card, "短剧");
            if (related.Id.Length > 0)
            {
                relatedDramas.Add(related);
            }
        }

        string pureText = ValueNormalizer.First(post.PureContent, post.Title, string.Empty);
        return new Post
        {
            Id = post.PostId,
            Title = ValueNormalizer.First(post.Title, pureText),
            PureText = pureText,
            Images = images,
            Topics = topics,
            Author = new User
            {
                Id = post.UserInfo.UserId,
                Name = post.UserInfo.UserName,
                AvatarUrl = post.UserInfo.UserAvatar,
            },
            CreatedAtUnixSeconds = Math.Max(0, post.CreateTime),
            LikeCount = Math.Max(0, post.DiggCount),
            CommentCount = Math.Max(0, post.ReplyCount),
            CollectCount = Math.Max(0, post.FavoriteCount),
            IsLiked = post.HasDigg,
            IsCollected = post.HasFavorite,
            RelatedDramas = relatedDramas,
        };
    }

    private static NovelCreationStatus MapNovelStatus(string? status) =>
        status switch
        {
            "0" => NovelCreationStatus.Completed,
            "1" => NovelCreationStatus.Ongoing,
            _ => NovelCreationStatus.Unknown,
        };

    private sealed class ParsedSearchTabData
    {
        public List<ComprehensiveSearchItem> Comprehensive { get; } = [];
        public List<Drama> Dramas { get; } = [];
        public List<DramaAlbum> Albums { get; } = [];
        public List<ShortVideo> ShortVideos { get; } = [];
        public List<VideoSearchItem> Videos { get; } = [];
        public List<Post> Discussions { get; } = [];
        public List<Novel> Novels { get; } = [];
        public List<User> Users { get; } = [];
        public List<Audiobook> Audiobooks { get; } = [];
        public int Offset { get; set; }
        public int NextOffset { get; set; }
        public bool HasMore { get; set; }
        public string SearchId { get; set; } = string.Empty;
        public string Passback { get; set; } = string.Empty;
    }

    [GeneratedRegex(@"^(.*?)[第\s]*([0-9零〇一二两兩三四五六七八九十百]+)\s*([季部])[\p{P}\s]*$")]
    private static partial Regex SeasonRegex();

    private sealed record CachedValue<T>(T Value, DateTimeOffset ExpiresAt);

    private sealed class SeasonGroup(string title, string key, string unit)
    {
        public string Title { get; } = title;
        public string Key { get; } = key;
        public string Unit { get; } = unit;
        public HashSet<int> Known { get; } = [];
        public int Maximum { get; set; }
    }
}
