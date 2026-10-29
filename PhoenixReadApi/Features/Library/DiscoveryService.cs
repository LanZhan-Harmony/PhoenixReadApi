using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>剧集分类、首页 Feed、关联剧集、书架字段和分享链接。</summary>
internal sealed class DiscoveryService : LibraryServiceBase
{
    internal DiscoveryService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<CategoryPage> GetCategoryAsync(
        CategoryQuery query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateCategory(query.Category);
        (string genre, string scene, string displayName) = CategoryInfo(
            query.Category == PhoenixReadApiCategory.All
                ? PhoenixReadApiCategory.RealDrama
                : query.Category
        );
        AppCategoryPayload payload = new()
        {
            RequestScene = scene,
            Offset = Math.Max(0, query.Offset),
            SessionId = query.SessionId ?? string.Empty,
            ClientRequestType = query.Offset > 0 ? 2 : 3,
            SelectedItems = new AppCategorySelectedItems
            {
                Genre = [genre],
                CategoryTheme = query.ThemeIds,
                CategoryRole = query.RoleIds,
                CategoryEpoch = query.EpochIds,
                OnlineTime = query.OnlineTimeIds,
                CreationStatus = query.StatusIds,
            },
        };
        AppResponse<AppCatalogData> response = await _appTransport
            .PostAsync<AppCategoryPayload, AppCatalogData>(
                "/reading/distribution/category/landpage/v/",
                payload,
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);
        AppCatalogData data = response.Data ?? new();
        return new CategoryPage
        {
            Items = [.. data.Items.Select(item => DramaMapper.FromDto(item, displayName))],
            NextOffset = data.NextOffset,
            SessionId = data.SessionId,
            HasMore = data.HasMore,
        };
    }

    internal async Task<FeedPage> GetNewSeriesAsync(
        int offset,
        string sessionId,
        CancellationToken cancellationToken
    )
    {
        if (offset < 0 || offset > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        try
        {
            AppResponse<AppCellChangeData> response = await _appTransport
                .GetAsync<AppCellChangeData>(
                    "/reading/bookapi/bookmall/cell/change/v1/",
                    [
                        new("selected_items", ""),
                        new("client_req_type", offset > 0 ? "2" : "3"),
                        new("cell_id", ""),
                        new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                        new("session_id", sessionId ?? string.Empty),
                    ],
                    cancellationToken
                )
                .ConfigureAwait(false);
            AppCellChangeData data = response.Data ?? new();
            return MapFeed(data.Cells, data.NextOffset, data.SessionId, data.HasMore);
        }
        catch (PhoenixReadApiProviderException exception) when (exception.ProviderCode == "100103")
        {
            return await GetFeedCategoriesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<FeedPage> GetFeedCategoriesAsync(CancellationToken cancellationToken)
    {
        AppResponse<AppFeedTabData> response = await _appTransport
            .GetAsync<AppFeedTabData>(
                "/reading/bookapi/bookmall/tab/v",
                [
                    new("bottom_tab_type", "0"),
                    new("landing_bottom_tab_type", "0"),
                    new("need_personal_recommend", "true"),
                    new("screen_width_px", "1080"),
                    new("is_horizontal_screen", "false"),
                    new("disable_digg_stat", "false"),
                    new("has_video_cache", "false"),
                    new("last_tab_index", "0"),
                    new("req_rank_category_id", ""),
                    new("video_type_preferences_str", ""),
                    new("tab_type", "0"),
                    new("client_req_type", "3"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppFeedTabData data = response.Data ?? new();
        List<FeedSection> sections = [];
        foreach (AppFeedTab tab in data.Tabs ?? [])
        {
            sections.AddRange(MapFeedSections(tab.Cells ?? [], tab.Title));
        }
        return new FeedPage { Sections = sections };
    }

    internal async Task<BookPackFields> GetBookPackFieldsAsync(
        string channel,
        CancellationToken cancellationToken
    )
    {
        channel = (channel ?? string.Empty).Trim();
        if (channel.Length == 0 || channel.Length > 64)
        {
            throw new ArgumentException("书架字段频道无效。", nameof(channel));
        }
        AppResponse<AppBookPackFieldsData> response;
        try
        {
            response = await _appTransport
                .PostAsync<AppBookPackFieldsPayload, AppBookPackFieldsData>(
                    "/reading/distribution/book_pack_fields/v1/",
                    new AppBookPackFieldsPayload { Channel = channel },
                    commentRequest: false,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (PhoenixReadApiProviderException exception) when (exception.ProviderCode == "100001")
        {
            return new BookPackFields();
        }
        return new BookPackFields
        {
            Values =
                response.Data?.Values ?? new Dictionary<string, string>(StringComparer.Ordinal),
        };
    }

    internal async Task<IReadOnlyList<Drama>> GetRelatedDramasAsync(
        string seriesId,
        int totalEpisodeCount,
        int currentEpisode,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        AppResponse<AppRelatedDramaData> response;
        try
        {
            response = await _appTransport
                .GetAsync<AppRelatedDramaData>(
                    "/reading/bookapi/plan/v",
                    [
                        new("book_id", seriesId),
                        new(
                            "total_chapter_num",
                            Math.Max(0, totalEpisodeCount).ToString(CultureInfo.InvariantCulture)
                        ),
                        new(
                            "current_chapter_num",
                            Math.Max(0, currentEpisode).ToString(CultureInfo.InvariantCulture)
                        ),
                        new("is_horizontal_screen", "false"),
                        new("bookstore_tab_type", "4"),
                    ],
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (PhoenixReadApiProviderException exception) when (exception.ProviderCode == "100103")
        {
            return [];
        }
        return
        [
            .. (response.Data?.Items ?? [])
                .SelectMany(FlattenRelatedDramas)
                .GroupBy(static item => item.Id, StringComparer.Ordinal)
                .Select(static group => group.First())
                .Select(static item => new Drama
                {
                    Id = item.Id,
                    Title = item.Title,
                    Description = item.Description,
                    CoverUrl = item.CoverUrl,
                    Category = item.Category,
                    EpisodeCount = item.EpisodeCount,
                }),
        ];
    }

    internal async Task<ShareLink> CreateShareLinkAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        videoId = NormalizeVideoId(videoId);
        AppResponse<AppShareData> response;
        try
        {
            response = await _appTransport
                .GetAsync<AppShareData>(
                    "/reading/user/share/short_url/unlimited_selector_change_type/",
                    [
                        new("book_id", seriesId),
                        new("video_id", videoId),
                        new("content_id", videoId),
                        new("src_material_id", seriesId),
                        new("source_channel", "short_video"),
                        new("share_type", "video"),
                        new("client_req_type", "3"),
                        new("unlimited_selector_change_type", "0"),
                        new(
                            "share_timestamp",
                            DateTimeOffset
                                .UtcNow.ToUnixTimeSeconds()
                                .ToString(CultureInfo.InvariantCulture)
                        ),
                    ],
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (PhoenixReadApiHttpException exception) when (exception.StatusCode == 404)
        {
            return new ShareLink { SeriesId = seriesId, VideoId = videoId };
        }
        catch (PhoenixReadApiProviderException exception) when (exception.ProviderCode == "100103")
        {
            return new ShareLink { SeriesId = seriesId, VideoId = videoId };
        }
        AppShareData data = response.Data ?? new();
        return new ShareLink
        {
            SeriesId = seriesId,
            VideoId = videoId,
            Url = data.Url,
            ShortUrl = data.ShortUrl,
            Title = data.Title,
        };
    }

    internal static ShareLink ResolveShareLink(string value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out Uri? uri))
        {
            throw new ArgumentException("分享链接不是有效的绝对 URL。", nameof(value));
        }

        Dictionary<string, string> query = ParseQuery(uri.Query);
        string seriesId = FirstQuery(query, "series_id", "book_id", "group_id");
        string videoId = FirstQuery(query, "video_id", "vid", "item_id");
        return new ShareLink
        {
            Url = uri.ToString(),
            ShortUrl = uri.ToString(),
            SeriesId = ValueNormalizer.IsNumericId(seriesId) ? seriesId : string.Empty,
            VideoId = ValueNormalizer.IsNumericId(videoId) ? videoId : string.Empty,
        };
    }

    private static FeedPage MapFeed(
        IReadOnlyList<AppFeedCell>? cells,
        int nextOffset,
        string sessionId,
        bool hasMore
    ) =>
        new()
        {
            Sections = MapFeedSections(cells ?? [], ""),
            NextOffset = nextOffset,
            SessionId = sessionId ?? string.Empty,
            HasMore = hasMore,
        };

    private static IEnumerable<AppRelatedDramaItem> FlattenRelatedDramas(AppRelatedDramaItem item)
    {
        yield return item;
        foreach (AppRelatedDramaItem child in item.Children.SelectMany(FlattenRelatedDramas))
        {
            yield return child;
        }
    }

    private static IReadOnlyList<FeedSection> MapFeedSections(
        IReadOnlyList<AppFeedCell> cells,
        string parentTitle
    )
    {
        List<FeedSection> sections = [];
        foreach (AppFeedCell cell in cells ?? [])
        {
            IReadOnlyList<DramaDto> videoItems = cell.VideoItems ?? [];
            IReadOnlyList<DramaDto> bookItems = cell.BookItems ?? [];
            IReadOnlyList<DramaDto> rows = videoItems.Count > 0 ? videoItems : bookItems;
            List<Drama> items =
            [
                .. rows.Select(item => DramaMapper.FromDto(item, "推荐"))
                    .Where(static item => item.Id.Length > 0),
            ];
            string title = cell.Name.Length > 0 ? cell.Name : parentTitle;
            if (items.Count > 0)
            {
                sections.Add(
                    new FeedSection
                    {
                        Id = cell.Id,
                        Title = title,
                        Items = items,
                    }
                );
            }
            sections.AddRange(MapFeedSections(cell.Children ?? [], title));
        }
        return sections;
    }
}
