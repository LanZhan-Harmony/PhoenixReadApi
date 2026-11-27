namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取当前客户端账号的短剧观看历史（服务端 book_type=2）。</summary>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>短剧观看历史和分页状态。</returns>
    public Task<HistoryPage<Drama>> GetDramaHistoryAsync(
        int offset = 0,
        int count = 20,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetDramaHistoryAsync(
            offset,
            count,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>获取当前客户端账号的合集观看历史（服务端 book_type=3）。</summary>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>合集观看历史和分页状态。</returns>
    public Task<HistoryPage<VideoAlbum>> GetVideoAlbumHistoryAsync(
        int offset = 0,
        int count = 20,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetVideoAlbumHistoryAsync(
            offset,
            count,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>获取当前客户端账号的独立短视频观看历史（服务端 book_type=4）。</summary>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>独立短视频观看历史和分页状态。</returns>
    public Task<HistoryPage<ShortVideo>> GetShortVideoHistoryAsync(
        int offset = 0,
        int count = 20,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetShortVideoHistoryAsync(
            offset,
            count,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>获取当前客户端账号的图文帖子浏览历史（服务端 book_type=5）。</summary>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>图文帖子浏览历史和分页状态。</returns>
    public Task<HistoryPage<Post>> GetPostHistoryAsync(
        int offset = 0,
        int count = 20,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetPostHistoryAsync(
            offset,
            count,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>获取当前客户端账号的小说与漫画阅读历史（服务端 book_type=0）。</summary>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>小说与漫画阅读历史和分页状态。</returns>
    public Task<HistoryPage<Novel>> GetNovelHistoryAsync(
        int offset = 0,
        int count = 20,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetNovelHistoryAsync(
            offset,
            count,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>获取当前客户端账号的有声听书历史（服务端 book_type=1）。</summary>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>有声听书历史和分页状态。</returns>
    public Task<HistoryPage<Audiobook>> GetAudiobookHistoryAsync(
        int offset = 0,
        int count = 20,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _history.GetAudiobookHistoryAsync(
            offset,
            count,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>上报单条粗粒度阅读/观看历史记录（走 read_history/update/v，支持全部 6 种内容类型）。</summary>
    /// <param name="update">历史记录更新描述符（可通过 <see cref="HistoryUpdate"/> 的静态工厂方法构造）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> ReportHistoryAsync(
        HistoryUpdate update,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(update);
        return _history.ReportHistoryAsync([update], cancellationToken);
    }

    /// <summary>批量上报一条或多条粗粒度阅读/观看历史记录（支持在同一次请求中混合多种类型）。</summary>
    /// <param name="updates">待上报的历史记录更新集合。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> ReportHistoryAsync(
        IEnumerable<HistoryUpdate> updates,
        CancellationToken cancellationToken = default
    ) => _history.ReportHistoryAsync(updates, cancellationToken);

    /// <summary>上报单条细粒度阅读/播放进度（走 read_progress/upload/v，支持小说、有声书、短剧、合集与短视频）。</summary>
    /// <param name="update">精细进度更新描述符（可通过 <see cref="ProgressUpdate"/> 的静态工厂方法构造）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> ReportProgressAsync(
        ProgressUpdate update,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(update);
        return _history.ReportProgressAsync([update], cancellationToken);
    }

    /// <summary>批量上报一条或多条细粒度阅读/播放进度。</summary>
    /// <param name="updates">待上报的精细进度更新集合。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> ReportProgressAsync(
        IEnumerable<ProgressUpdate> updates,
        CancellationToken cancellationToken = default
    ) => _history.ReportProgressAsync(updates, cancellationToken);

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
}
