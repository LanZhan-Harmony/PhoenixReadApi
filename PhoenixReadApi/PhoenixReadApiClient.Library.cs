namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取当前客户端账号的短剧观看历史（服务端 book_type=2）。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>短剧观看历史和分页状态。</returns>
    public Task<HistoryPage<Drama>> GetDramaHistoryAsync(
        int count = 20,
        int offset = 0,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetDramaHistoryAsync(
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>获取当前客户端账号的合集观看历史（服务端 book_type=3）。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>合集观看历史和分页状态。</returns>
    public Task<HistoryPage<DramaAlbum>> GetAlbumHistoryAsync(
        int count = 20,
        int offset = 0,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetAlbumHistoryAsync(
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>获取当前客户端账号的独立短视频观看历史（服务端 book_type=4）。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>独立短视频观看历史和分页状态。</returns>
    public Task<HistoryPage<ShortVideo>> GetShortVideoHistoryAsync(
        int count = 20,
        int offset = 0,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetShortVideoHistoryAsync(
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>获取当前客户端账号的图文帖子浏览历史（服务端 book_type=5）。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>图文帖子浏览历史和分页状态。</returns>
    public Task<HistoryPage<Post>> GetPostHistoryAsync(
        int count = 20,
        int offset = 0,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetPostHistoryAsync(
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>获取当前客户端账号的小说与漫画阅读历史（服务端 book_type=0）。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>小说与漫画阅读历史和分页状态。</returns>
    public Task<HistoryPage<Novel>> GetNovelHistoryAsync(
        int count = 20,
        int offset = 0,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetNovelHistoryAsync(
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>获取当前客户端账号的有声听书历史（服务端 book_type=1）。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>有声听书历史和分页状态。</returns>
    public Task<HistoryPage<Audiobook>> GetAudiobookHistoryAsync(
        int count = 20,
        int offset = 0,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetAudiobookHistoryAsync(
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>批量删除一条或多条阅读/观看历史（支持在同一次请求中混合多种类型）。</summary>
    /// <param name="entries">待删除的历史条目集合（可通过 <see cref="HistoryDeleteEntry"/> 的静态工厂方法构造）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteHistoryAsync(
        IEnumerable<HistoryDeleteEntry> entries,
        CancellationToken cancellationToken = default
    ) => _history.DeleteHistoryAsync(entries, cancellationToken);

    /// <summary>直接根据强类型历史记录项删除对应历史记录。</summary>
    /// <typeparam name="T">历史记录内容类型。</typeparam>
    /// <param name="item">由任意 <c>Get*HistoryAsync</c> 接口返回的记录项。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteHistoryAsync<T>(
        HistoryItem<T> item,
        CancellationToken cancellationToken = default
    ) => _history.DeleteHistoryAsync([HistoryDeleteEntry.FromItem(item)], cancellationToken);

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
