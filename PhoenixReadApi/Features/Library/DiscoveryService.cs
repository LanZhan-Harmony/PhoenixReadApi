using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
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

    internal async Task<ShareLink> CreateShareLinkAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        videoId = NormalizeVideoId(videoId);
        string fallbackUrl =
            videoId.Length > 0
                ? $"https://fanqienovel.com/drama/{seriesId}?video_id={videoId}"
                : $"https://fanqienovel.com/drama/{seriesId}";
        AppResponse<AppShareData>? response = null;
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
        catch (PhoenixReadApiHttpException exception) when (exception.StatusCode == 404) { }
        AppShareData data = response?.Data ?? new();
        string url = ValueNormalizer.First(data.Url, data.ShortUrl, fallbackUrl);
        string shortUrl = ValueNormalizer.First(data.ShortUrl, data.Url, fallbackUrl);
        return new ShareLink
        {
            SeriesId = seriesId,
            VideoId = videoId,
            Url = url,
            ShortUrl = shortUrl,
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
