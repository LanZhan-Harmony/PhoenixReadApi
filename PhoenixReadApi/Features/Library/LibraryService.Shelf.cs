using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Library;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>阅读历史、进度、收藏、点赞、预约和分类等 App 接口。</summary>
internal sealed partial class LibraryService
{
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

    internal async Task<DramaPage> GetLikesAsync(
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppUserActionData> response = await _appTransport
            .GetAsync<AppUserActionData>(
                "/reading/ugc/action/mget/v",
                [
                    new("count", count.ToString(CultureInfo.InvariantCulture)),
                    new("object_type_list", "6,15,10"),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("action_type", "3"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppUserActionData data = response.Data ?? new();
        return new DramaPage
        {
            Items =
            [
                .. (data.Items ?? [])
                    .Where(static item => item is not null)
                    .Select(MapLike)
                    .Where(static item => item.Id.Length > 0),
            ],
            Total = data.Total,
            NextOffset = data.NextOffset,
            HasMore = data.HasMore,
        };
    }

    internal async Task<FollowUpdatePage> GetFollowUpdatesAsync(
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        ValidateCount(count);
        cursor = NormalizeCursor(cursor);
        AppResponse<AppFollowData> response = await _appTransport
            .GetAsync<AppFollowData>(
                "/novel/articleapi/relation/get_user_action/v1/",
                [
                    new("action_channel", "1"),
                    new("count", count.ToString(CultureInfo.InvariantCulture)),
                    new("cursor_str", cursor),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppFollowData data = response.Data ?? new();
        AppFollowListInfo listInfo = data.ListInfo ?? new();
        return new FollowUpdatePage
        {
            Items = [.. data.Items.Select(MapFollow)],
            Cursor = listInfo.Cursor,
            HasMore = listInfo.HasMore,
            Total = data.Items.Count,
        };
    }

    internal async Task<ReservationPage> GetReservationsAsync(
        int count,
        int offset,
        bool calendar,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppReservationData> response = await _appTransport
            .GetAsync<AppReservationData>(
                "/reading/user/subscribe/list/v:version/",
                [
                    new("bdhm_bid", "novelread_lynx"),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("active_panel", "2"),
                    new("session_id", string.Empty),
                    new("version", "1"),
                    new("gender_type", "2"),
                    new("bdhm_pid", "subscribe-list"),
                    new("tab_type", "1"),
                    new("subscribe_list_opt", "0"),
                    new("filter_type", "gender"),
                    new("limit", count.ToString(CultureInfo.InvariantCulture)),
                    new("tab_style", "2"),
                    new("device_platform", "android"),
                    new("is_online", calendar ? "true" : "false"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        return MapReservations(response.Data ?? new());
    }

    internal async Task<ReservationPage> GetUpcomingReservationsAsync(
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppReservationData> response = await _appTransport
            .GetAsync<AppReservationData>(
                "/reading/user/subscribe/list/v:version/",
                [
                    new("tab_type", "2"),
                    new("req_type", "panel_only"),
                    new("limit", count.ToString(CultureInfo.InvariantCulture)),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("session_id", string.Empty),
                    new("version", "0"),
                    new("gender_type", "2"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        return MapReservations(response.Data ?? new());
    }

    internal Task<PhoenixOperationResult> SetReservationAsync(
        string seriesId,
        bool active,
        CancellationToken cancellationToken
    ) => SetReservationCoreAsync(seriesId, active, cancellationToken);
}
