using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>剧集预约、预约列表和预约日历。</summary>
internal sealed class ReservationsService : LibraryServiceBase
{
    internal ReservationsService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    private async Task<PhoenixOperationResult> SetReservationCoreAsync(
        string seriesId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppReservationActionPayload, AppOperationResponse>(
                "/reading/bookapi/search/uncover_subscribe/v",
                new AppReservationActionPayload
                {
                    ItemId = seriesId,
                    ItemType = 14,
                    OperationType = active ? 1 : 2,
                    WishListAllDelete = 0,
                    NotDeleteItemId = string.Empty,
                    SharkParameter = new AppSharkParameter
                    {
                        EnterFrom = "ShortSeriesActivity",
                        PreviousPage = "MainFragmentActivity",
                        PageList = "MainFragmentActivity,ShortSeriesActivity",
                    },
                },
                commentRequest: false,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
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
                    new("limit", count.ToString(CultureInfo.InvariantCulture)),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("session_id", string.Empty),
                    new("version", "1"),
                    new("gender_type", "2"),
                    new("device_platform", "android"),
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

    private static ReservationPage MapReservations(AppReservationData data)
    {
        IReadOnlyList<AppReservationItem> items =
            data.Items is { Count: > 0 } ? data.Items
            : data.SubscribeItems is { Count: > 0 } ? data.SubscribeItems
            : data.LegacySubscribeItems ?? [];
        return new ReservationPage
        {
            Items = [.. items.Select(MapReservation)],
            NextOffset = data.NextOffset,
            HasMore = data.HasMore,
            Total = Math.Max(
                items.Count,
                Math.Max(data.Total, Math.Max(data.OnlineTotalCount, data.OfflineTotalCount))
            ),
        };
    }

    private static Reservation MapReservation(AppReservationItem item) =>
        new()
        {
            Id = ValueNormalizer.First(item.Id, item.ItemId),
            SeriesId = ValueNormalizer.First(item.SeriesId, item.ItemId),
            VideoId = item.VideoId ?? string.Empty,
            Title = ValueNormalizer.First(item.Title, item.Name),
            CoverUrl = item.CoverUrl ?? string.Empty,
            ReservedAtUnixSeconds = Math.Max(0, item.ReservedAt),
            ReleaseAtUnixSeconds = Math.Max(item.ReleaseAt, item.ExpectedPublishAt),
            IsActive = item.IsOnline || item.ReleaseAt > 0,
        };
}
