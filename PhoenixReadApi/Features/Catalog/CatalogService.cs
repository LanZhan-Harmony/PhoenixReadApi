using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhoenixReadApi.Contracts.App.Catalog;
using PhoenixReadApi.Contracts.Web.Catalog;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Infrastructure.Web;

namespace PhoenixReadApi.Internal.Features.Catalog;

/// <summary>Coordinates catalog sources, pagination state, merging and web fallback.</summary>
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
        CatalogResult result = await GetResultAsync(category, cancellationToken)
            .ConfigureAwait(false);
        return result.Items;
    }

    internal Task<CatalogResult> GetResultAsync(
        PhoenixReadApiCategory category,
        CancellationToken cancellationToken
    ) => GetResultCoreAsync(category, cancellationToken);

    private async Task<CatalogResult> GetResultCoreAsync(
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
            var usedWebFallback = category == PhoenixReadApiCategory.Comic;
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
                    string.Compare(left.DisplayTitle, right.DisplayTitle, StringComparison.Ordinal)
            );
            if (items.Count == 0 && warnings.Count > 0)
            {
                throw new PhoenixReadApiFallbackException(
                    "无法获取红果剧库。" + string.Join("；", warnings),
                    "catalog"
                );
            }

            return new CatalogResult
            {
                Items = items,
                Category = category,
                UsedWebFallback = usedWebFallback,
                Warnings = warnings,
            };
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
        for (var page = 0; page < _options.MaxPagesPerCategory && !cursor.Exhausted; page++)
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
            foreach (AppDramaDto row in data.Items)
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

            var hasMore = data.HasMore;
            var nextOffset =
                data.NextOffset > 0 ? data.NextOffset : cursor.Offset + data.Items.Count;
            var pageSignature = string.Join(
                "\n",
                pageItems
                    .Select(static item => item.Id)
                    .OrderBy(static id => id, StringComparer.Ordinal)
            );
            var signature =
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

            var session = data.SessionId ?? string.Empty;
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
        for (var page = 1; page <= _options.MaxPagesPerCategory; page++)
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
        foreach (AppDramaDto item in data.Items)
        {
            Drama drama = DramaMapper.FromDto(item, category);
            if (drama.Id.Length > 0)
            {
                output.Add(drama);
            }
        }

        var total = data.Pagination?.TotalPages ?? 0;
        return (output, Math.Clamp(total <= 0 ? page : total, page, 500));
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
