namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取当前客户端账号的阅读历史。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>阅读历史和分页状态。</returns>
    /// <remarks>红果会把短剧历史和其他阅读历史放在同一接口中，客户端只保留可识别的视频记录。</remarks>
    public Task<ReadHistoryPage> GetHistoryAsync(
        int count = 20,
        int offset = 0,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetHistoryAsync(
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>删除当前账号的一条短剧阅读历史。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteHistoryAsync(
        string dramaId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _history.DeleteHistoryAsync(dramaId, videoId, cancellationToken);

    /// <summary>上报当前分集的播放进度。</summary>
    /// <param name="update">视频、剧集和播放位置；UseSoftDelete 为 true 时会删除历史。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> ReportProgressAsync(
        ProgressUpdate update,
        CancellationToken cancellationToken = default
    ) => _history.ReportProgressAsync(update, cancellationToken);

    /// <summary>获取当前客户端账号收藏的剧集。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏剧集和分页状态。</returns>
    /// <remarks>当前 App 收藏列表只返回剧集 ID，Title、Description 和 CoverUrl 可能为空；需要完整信息时再调用 GetDramaDetailsAsync。</remarks>
    public Task<DramaPage> GetFavoritesAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _favorite.GetFavoritesAsync(count, offset, cancellationToken);

    /// <summary>收藏或取消收藏一部剧。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">用于定位收藏记录的分集视频 ID。</param>
    /// <param name="active">true 表示收藏，false 表示取消收藏。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetFavoriteAsync(
        string dramaId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _favorite.SetFavoriteAsync(dramaId, videoId, active, cancellationToken);

    /// <summary>获取当前客户端账号点赞的剧集。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>点赞剧集和分页状态。</returns>
    public Task<DramaPage> GetLikesAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _like.GetLikesAsync(count, offset, cancellationToken);

    /// <summary>点赞或取消点赞一部剧的一集。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">用于定位剧集的分集视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetDramaEpisodeLikeAsync(
        string dramaId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _like.SetDramaEpisodeLikeAsync(dramaId, videoId, active, cancellationToken);

    /// <summary>获取未上线预约的剧集。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>预约记录和分页状态。</returns>
    public Task<ReservationPage> GetPendingReservationsAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _reservation.GetReservationsAsync(count, offset, isAvailable: false, cancellationToken);

    /// <summary>获取已上线预约的剧集。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>预约记录；服务端提供的日历字段已转换为统一的预约实体。</returns>
    public Task<ReservationPage> GetAvailableReservationsAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _reservation.GetReservationsAsync(count, offset, isAvailable: true, cancellationToken);

    /// <summary>获取可预约的新剧列表。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">分页偏移量。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>可预约剧集及分页状态。</returns>
    public Task<ReservationPage> GetUpcomingReservationsAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _reservation.GetUpcomingReservationsAsync(count, offset, cancellationToken);

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
