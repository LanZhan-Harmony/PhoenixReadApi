using System.Globalization;
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

    private readonly record struct CatalogCursor(
        int Offset,
        string SessionId,
        string LastId,
        string PageSignature,
        bool Initialized,
        bool Exhausted,
        DateTimeOffset UpdatedAt
    );
}
