using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PhoenixReadApi.Contracts.App.Catalog;
using PhoenixReadApi.Contracts.Web.Search;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Search;

/// <summary>负责搜索、联想、季数补齐和推荐分页。</summary>
internal sealed partial class SearchService : PhoenixFeatureService
{
    private const int SearchSuggestionCount = 5;
    private const int MaxSuggestions = 5;
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SuggestionCacheLifetime = TimeSpan.FromMinutes(2);
    private readonly Lock _stateLock = new();
    private readonly Dictionary<string, CachedValue<SearchResult>> _searchCache = new(
        StringComparer.Ordinal
    );
    private readonly Dictionary<string, CachedValue<List<SearchSuggestion>>> _suggestionCache = new(
        StringComparer.Ordinal
    );
    private readonly ConcurrentDictionary<string, Lazy<Task<SearchResult>>> _searchPending = new(
        StringComparer.Ordinal
    );
    private readonly ConcurrentDictionary<
        string,
        Lazy<Task<IReadOnlyList<SearchSuggestion>>>
    > _suggestionPending = new(StringComparer.Ordinal);

    internal SearchService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal Task<SearchResult> SearchAsync(string keyword, CancellationToken cancellationToken)
    {
        var query = NormalizeKeyword(keyword);
        if (TryGetCache(_searchCache, query, out SearchResult? cached) && cached is not null)
        {
            return Task.FromResult(cached);
        }

        Lazy<Task<SearchResult>> pending = _searchPending.GetOrAdd(
            query,
            static (value, state) =>
                new Lazy<Task<SearchResult>>(
                    () => state.SearchCoreAsync(value),
                    LazyThreadSafetyMode.ExecutionAndPublication
                ),
            this
        );
        return AwaitSearchAsync(query, pending, cancellationToken);
    }

    private async Task<SearchResult> AwaitSearchAsync(
        string query,
        Lazy<Task<SearchResult>> pending,
        CancellationToken cancellationToken
    )
    {
        try
        {
            SearchResult result = await pending
                .Value.WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            if (result.Warning.Length == 0)
            {
                lock (_stateLock)
                {
                    _searchCache[query] = new CachedValue<SearchResult>(
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
                new KeyValuePair<string, Lazy<Task<SearchResult>>>(query, pending)
            );
        }
    }

    internal Task<IReadOnlyList<SearchSuggestion>> GetSuggestionsAsync(
        string keyword,
        CancellationToken cancellationToken
    )
    {
        var query = NormalizeKeyword(keyword);
        if (
            TryGetCache(_suggestionCache, query, out List<SearchSuggestion>? cached)
            && cached is not null
        )
        {
            return Task.FromResult<IReadOnlyList<SearchSuggestion>>(cached);
        }

        Lazy<Task<IReadOnlyList<SearchSuggestion>>> pending = _suggestionPending.GetOrAdd(
            query,
            static (value, state) =>
                new Lazy<Task<IReadOnlyList<SearchSuggestion>>>(
                    () => state.LoadSuggestionsAsync(value),
                    LazyThreadSafetyMode.ExecutionAndPublication
                ),
            this
        );
        return AwaitSuggestionsAsync(query, pending, cancellationToken);
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

    internal Task<RecommendationPage> GetRecommendationsAsync(
        RecommendationQuery query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateRecommendationQuery(query);
        return GetRecommendationsCoreAsync(query, cancellationToken);
    }

    private async Task<SearchResult> SearchCoreAsync(string keyword)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(40));
        CancellationToken cancellationToken = timeout.Token;
        List<Drama> names = [];
        Exception? namesError = null;
        try
        {
            names = await FetchSearchNamesAsync(keyword, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            namesError = exception;
        }

        SearchResult page = new();
        Exception? pageError = null;
        try
        {
            using CancellationTokenSource pageTimeout =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            pageTimeout.CancelAfter(TimeSpan.FromSeconds(12));
            page = await FetchSearchPageAsync(keyword, pageTimeout.Token).ConfigureAwait(false);
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
        var limited = page.Limited || pageError is not null || namesError is not null;
        var warning = string.Empty;
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
            var seasonLimited = await CompleteSearchSeasonsAsync(keyword, merged, cancellationToken)
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
        return new SearchResult
        {
            Items = merged,
            Total = Math.Max(page.Total, merged.Count),
            Limited = limited,
            Warning = warning,
        };
    }

    private async Task<List<Drama>> FetchSearchNamesAsync(
        string keyword,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<SuggestionRecord> records = await FetchSuggestionRecordsAsync(
                keyword,
                SearchSuggestionCount,
                cancellationToken
            )
            .ConfigureAwait(false);
        List<Drama> output = [];
        foreach (SuggestionRecord record in records)
        {
            if (!string.Equals(record.WordType, "short_play_name", StringComparison.Ordinal))
            {
                continue;
            }

            AppDramaDto? data = record.VideoData;
            if (
                data is null
                || !JsonValueReader.IsNumericId(
                    JsonValueReader.First(data.SeriesId, data.SeriesIdNumber)
                )
            )
            {
                continue;
            }

            Drama drama = DramaMapper.FromDto(data, "短剧");
            if (drama.DisplayTitle == drama.SourceId && !string.IsNullOrWhiteSpace(record.Name))
            {
                var title = record.Name.Trim();
                drama = new Drama
                {
                    Id = drama.Id,
                    Source = drama.Source,
                    SourceId = drama.SourceId,
                    Title = title,
                    Name = title,
                    Description = drama.Description,
                    CoverUrl = drama.CoverUrl,
                    Category = drama.Category,
                    Channel = drama.Channel,
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

            if (drama.Id.Length > 0 && drama.DisplayTitle != drama.SourceId)
            {
                output.Add(drama);
            }
        }

        return MergeDramas([], output);
    }

    private async Task<SearchResult> FetchSearchPageAsync(
        string keyword,
        CancellationToken cancellationToken
    )
    {
        Uri uri = new(_options.WebBaseUri, "search/" + Uri.EscapeDataString(keyword));
        using JsonDocument? document = RouterDataExtractor.Parse(
            await GetTextAsync(uri, _options.WebBaseUri, "search", cancellationToken)
                .ConfigureAwait(false)
        );
        WebSearchPageDto? page = RouterDataLocator.Deserialize<WebSearchPageDto>(
            document,
            "search_(keyword)/page",
            "search_"
        );
        if (page is null || !page.IsSuccess || page.Query != keyword)
        {
            throw new PhoenixReadApiResponseException("红果搜索未返回有效结果。", "search");
        }

        List<Drama> output = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (AppDramaDto row in page.Items)
        {
            Drama drama = DramaMapper.FromDto(row, "短剧");
            if (drama.Id.Length > 0 && seen.Add(drama.Id))
            {
                output.Add(drama);
            }
        }

        if (page.Items.Count > 0 && output.Count == 0)
        {
            throw new PhoenixReadApiResponseException("搜索结果中没有可识别的剧集。", "search");
        }

        return new SearchResult
        {
            Items = output,
            Total = Math.Max(page.TotalCount, output.Count),
            Limited = page.TotalCount > output.Count,
        };
    }

    private async Task<IReadOnlyList<SearchSuggestion>> LoadSuggestionsAsync(string query)
    {
        IReadOnlyList<SuggestionRecord> records = await FetchSuggestionRecordsAsync(
                query,
                MaxSuggestions,
                CancellationToken.None
            )
            .ConfigureAwait(false);
        List<SearchSuggestion> output = new(MaxSuggestions);
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (SuggestionRecord record in records)
        {
            var name = record.Name?.Trim() ?? string.Empty;
            if (name.Length is 0 or > 80 || name.Any(char.IsControl) || !seen.Add(name))
            {
                continue;
            }

            var type = record.WordType
                is "short_play_name"
                    or "short_play_category"
                    or "common_query"
                    or "actor_name"
                    or "short_play_actor"
                ? record.WordType
                : string.Empty;
            output.Add(new SearchSuggestion { Name = name, Type = type });
            if (output.Count == MaxSuggestions)
            {
                break;
            }
        }

        return output;
    }

    private async Task<IReadOnlyList<SuggestionRecord>> FetchSuggestionRecordsAsync(
        string query,
        int count,
        CancellationToken cancellationToken
    )
    {
        Uri uri = new(
            _options.WebBaseUri,
            $"incent_resource/suggestion?app_id=8662&query={Uri.EscapeDataString(query)}&count={count.ToString(CultureInfo.InvariantCulture)}"
        );
        var body = await GetTextAsync(
                uri,
                _options.WebBaseUri,
                "suggestions",
                cancellationToken,
                256 * 1024
            )
            .ConfigureAwait(false);
        try
        {
            SuggestionData? data = JsonAotSerializer.Deserialize<SuggestionData>(body);
            IReadOnlyList<SuggestionRecord>? items = data?.SuggestList;
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

    private async Task<RecommendationPage> GetRecommendationsCoreAsync(
        RecommendationQuery query,
        CancellationToken cancellationToken
    )
    {
        (string genre, string scene, string category) = CategoryInfo(query.Genre);
        var seen = query
            .Seen.Select(static id =>
                id.StartsWith("hongguo:", StringComparison.Ordinal) ? id[8..] : id
            )
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        AppCatalogPayload payload = new()
        {
            RequestScene = scene,
            Offset = query.Offset,
            ClientRequestType = query.Offset > 0 ? 2 : 3,
            SessionId = query.SessionId,
            FilterIds = string.Join(',', seen),
            SelectItems = new AppCatalogSelectItems { Genre = [genre] },
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
            ?? throw new PhoenixReadApiResponseException(
                "红果推荐数据格式异常。",
                "recommendations"
            );

        List<Drama> output = [];
        HashSet<string> seenIds = new(seen, StringComparer.Ordinal);
        foreach (AppDramaDto row in data.Items)
        {
            Drama drama = DramaMapper.FromDto(row, category);
            if (drama.Id.Length > 0 && seenIds.Add(drama.SourceId))
            {
                output.Add(drama);
            }
        }

        if (data.Items.Count > 0 && output.Count == 0)
        {
            throw new PhoenixReadApiResponseException(
                "红果推荐未返回可识别的剧集。",
                "recommendations"
            );
        }

        var hasMore = data.HasMore;
        var next = data.NextOffset;
        if (hasMore && (next <= query.Offset || next > 1_000_000 || output.Count == 0))
        {
            throw new PhoenixReadApiResponseException("红果推荐分页未前进。", "recommendations");
        }

        var session = data.SessionId ?? string.Empty;
        if (session.Length > 4096 || session.Any(static c => c is '\r' or '\n' or '\0'))
        {
            throw new PhoenixReadApiResponseException("红果推荐分页标记无效。", "recommendations");
        }

        if (!hasMore && (next < query.Offset || next > 1_000_000))
        {
            next = query.Offset;
        }

        return new RecommendationPage
        {
            Items = output,
            NextOffset = next,
            HasMore = hasMore,
            SessionId = session,
        };
    }

    private async Task<bool> CompleteSearchSeasonsAsync(
        string keyword,
        List<Drama> dramas,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, SeasonGroup> groups = new(StringComparer.Ordinal);
        foreach (Drama drama in dramas)
        {
            if (
                !TryParseSeason(drama.DisplayTitle, out var baseTitle, out var season, out var unit)
            )
            {
                continue;
            }

            var key = SearchText(baseTitle) + ":" + unit;
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
        HashSet<string> attempted = new(StringComparer.OrdinalIgnoreCase) { keyword };
        var requestCount = 0;
        foreach (SeasonGroup group in incomplete)
        {
            while (requestCount < 32 && !timeout.IsCancellationRequested)
            {
                var missing = Enumerable
                    .Range(1, group.Maximum)
                    .FirstOrDefault(number => !group.Known.Contains(number));
                if (missing == 0)
                {
                    break;
                }

                var labels = new[]
                {
                    ChineseSeason(missing),
                    missing.ToString(CultureInfo.InvariantCulture),
                };
                var queries =
                    missing == 1
                        ? new[]
                        {
                            group.Title,
                            group.Title + "第" + labels[0] + group.Unit,
                            group.Title + "第1" + group.Unit,
                        }
                        :
                        [
                            group.Title + "第" + labels[0] + group.Unit,
                            group.Title + "第" + labels[1] + group.Unit,
                        ];
                var query = queries.FirstOrDefault(attempted.Add);
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
                            match.DisplayTitle,
                            out var baseTitle,
                            out var season,
                            out var unit
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
        var leftRank = TitleRank(left.DisplayTitle, query);
        var rightRank = TitleRank(right.DisplayTitle, query);
        if (leftRank != rightRank)
        {
            return leftRank.CompareTo(rightRank);
        }

        if (
            TryParseSeason(
                left.DisplayTitle,
                out var leftBase,
                out var leftSeason,
                out var leftUnit
            )
            && TryParseSeason(
                right.DisplayTitle,
                out var rightBase,
                out var rightSeason,
                out var rightUnit
            )
            && leftUnit == rightUnit
            && SearchText(leftBase) == SearchText(rightBase)
        )
        {
            return leftSeason.CompareTo(rightSeason);
        }

        return string.Compare(left.DisplayTitle, right.DisplayTitle, StringComparison.Ordinal);
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
            var label = match.Success ? match.Groups[2].Value : string.Empty;
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
            var number = 0;
            foreach (var c in value)
            {
                if (!digits.TryGetValue(c, out var digit))
                {
                    return 0;
                }

                number = number * 10 + digit;
            }
            return number;
        }

        var result = 0;
        var current = 0;
        var previousUnit = 1000;
        foreach (var c in value)
        {
            if (digits.TryGetValue(c, out var digit))
            {
                current = current * 10 + digit;
                continue;
            }

            var unit = c == '百' ? 100 : 10;
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
        var digits = "零一二三四五六七八九";
        if (number < 10)
        {
            return digits[number].ToString();
        }

        if (number < 100)
        {
            var prefix = number >= 20 ? digits[number / 10].ToString() : string.Empty;
            return prefix
                + "十"
                + (number % 10 == 0 ? string.Empty : digits[number % 10].ToString());
        }

        var hundreds = digits[number / 100] + "百";
        var rest = number % 100;
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
        var normalizedTitle = SearchText(title);
        var normalizedQuery = SearchText(query);
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

        var words = query
            .Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
            .Select(SearchText)
            .ToArray();
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
