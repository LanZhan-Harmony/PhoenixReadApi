namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取未上线预约的剧集。</summary>
    /// <param name="offset">偏移量。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>预约记录和分页状态。</returns>
    public Task<ReservationPage> GetPendingReservationsAsync(
        int offset = 0,
        int count = 20,
        CancellationToken cancellationToken = default
    ) => _reservation.GetReservationsAsync(offset, count, isAvailable: false, cancellationToken);

    /// <summary>获取已上线预约的剧集。</summary>
    /// <param name="offset">偏移量。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>预约记录；服务端提供的日历字段已转换为统一的预约实体。</returns>
    public Task<ReservationPage> GetAvailableReservationsAsync(
        int offset = 0,
        int count = 20,
        CancellationToken cancellationToken = default
    ) => _reservation.GetReservationsAsync(offset, count, isAvailable: true, cancellationToken);

    /// <summary>获取可预约的新剧列表。</summary>
    /// <param name="offset">分页偏移量。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>可预约剧集及分页状态。</returns>
    public Task<ReservationPage> GetUpcomingReservationsAsync(
        int offset = 0,
        int count = 20,
        CancellationToken cancellationToken = default
    ) => _reservation.GetUpcomingReservationsAsync(offset, count, cancellationToken);

    /// <summary>设置或取消剧集预约。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="active">true 表示预约，false 表示取消预约。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetReservationAsync(
        string dramaId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _reservation.SetReservationAsync(dramaId, active, cancellationToken);

    /// <summary>取消剧集预约。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteReservationAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    ) => _reservation.SetReservationAsync(dramaId, active: false, cancellationToken);
}
