using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>剧集收藏列表以及收藏和取消收藏操作。</summary>
internal sealed class FavoritesService : LibraryServiceBase
{
    internal FavoritesService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<PhoenixOperationResult> SetFavoriteAsync(
        string seriesId,
        string videoId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        videoId = NormalizeVideoId(videoId);
        AppShelfUpdatePayload payload = new()
        {
            IsActiveBehavior = true,
            OperationType = active ? 1 : 2,
            Items = [new AppShelfUpdateItem { SeriesId = seriesId, VideoId = videoId }],
        };
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppShelfUpdatePayload, AppOperationResponse>(
                "/reading/bookapi/bookshelf/video/update/v",
                payload,
                commentRequest: false,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    internal async Task<DramaPage> GetFavoritesAsync(
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppShelfListData> response = await _appTransport
            .GetAsync<AppShelfListData>(
                "/reading/bookapi/bookshelf/video/list/v",
                [
                    new("count", count.ToString(CultureInfo.InvariantCulture)),
                    new("limit", count.ToString(CultureInfo.InvariantCulture)),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("tab_type", "4"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppShelfListData data = response.Data ?? new();
        IReadOnlyList<AppShelfItem> shelfItems = data.ShelfItems ?? [];
        IReadOnlyList<AppShelfItem> listItems = data.Items ?? [];
        IReadOnlyList<AppShelfItem> items = shelfItems.Count > 0 ? shelfItems : listItems;
        return new DramaPage
        {
            Items = [.. items.Select(MapShelf).Where(static item => item.Id.Length > 0)],
            Total = data.Total > 0 ? data.Total : items.Count,
            NextOffset = data.NextOffset,
            HasMore = data.HasMore,
        };
    }

    private static Drama MapShelf(AppShelfItem item)
    {
        AppShelfVideoData video = item.Video ?? new();
        string seriesId = ValueNormalizer.First(item.SeriesId, video.SeriesId);
        // The real bookshelf list currently returns only series_id for short dramas.
        // Keep missing metadata empty instead of presenting the ID as a title; callers
        // can request full details with GetDramaDetailsAsync(seriesId).
        return new Drama
        {
            Id = seriesId,
            Title = item.Title,
            Description = item.Description,
            CoverUrl = item.CoverUrl,
            EpisodeCount = video.EpisodeCount,
        };
    }
}
