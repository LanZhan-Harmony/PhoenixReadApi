using PhoenixReadApi.Internal.Features.Library;

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

    /// <summary>获取当前客户端的所有短剧播放进度。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端保存的播放进度。</returns>
    public Task<ReadProgressPage> GetReadProgressAsync(
        CancellationToken cancellationToken = default
    ) => _history.GetReadProgressAsync(cancellationToken);

    /// <summary>按剧集或视频 ID 查询播放进度。</summary>
    /// <param name="seriesOrVideoIds">剧集 ID 或视频 ID，至少一个，最多 200 个。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>与输入 ID 匹配的播放进度。</returns>
    /// <remarks>调用方可以把搜索或详情返回的 ID 原样传入，不需要添加前缀。</remarks>
    public Task<ReadProgressPage> GetReadProgressForAsync(
        IReadOnlyList<string> seriesOrVideoIds,
        CancellationToken cancellationToken = default
    ) => _history.GetReadProgressForAsync(seriesOrVideoIds, cancellationToken);

    /// <summary>上报当前分集的播放进度。</summary>
    /// <param name="update">视频、剧集和播放位置；UseSoftDelete 为 true 时会删除历史。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> ReportProgressAsync(
        ProgressUpdate update,
        CancellationToken cancellationToken = default
    ) => _history.ReportProgressAsync(update, cancellationToken);

    /// <summary>删除当前账号的一条短剧阅读历史。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteHistoryAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _history.DeleteHistoryAsync(seriesId, videoId, cancellationToken);

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
    ) => _favorites.GetFavoritesAsync(count, offset, cancellationToken);

    /// <summary>获取当前账号收藏剧集的 ID。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏剧集 ID 和分页状态。</returns>
    public async Task<(IReadOnlyList<string> Ids, DramaPage Page)> GetFavoriteIdsAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    )
    {
        DramaPage page = await GetFavoritesAsync(count, offset, cancellationToken)
            .ConfigureAwait(false);
        return ([.. page.Items.Select(static item => item.Id)], page);
    }

    /// <summary>取消收藏一部剧。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="videoId">用于定位收藏记录的分集视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> RemoveFavoriteAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _favorites.SetFavoriteAsync(seriesId, videoId, active: false, cancellationToken);

    /// <summary>获取当前客户端账号点赞的剧集。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>点赞剧集和分页状态。</returns>
    public Task<DramaPage> GetLikesAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _likes.GetLikesAsync(count, offset, cancellationToken);

    /// <summary>获取预约中的剧集。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>预约记录和分页状态。</returns>
    public Task<ReservationPage> GetReservationsAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _reservations.GetReservationsAsync(count, offset, calendar: false, cancellationToken);

    /// <summary>获取预约日历数据。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>预约记录；服务端提供的日历字段已转换为统一的预约实体。</returns>
    public Task<ReservationPage> GetCalendarAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _reservations.GetReservationsAsync(count, offset, calendar: true, cancellationToken);

    /// <summary>获取可预约的新剧列表。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">分页偏移量。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>可预约剧集及分页状态。</returns>
    public Task<ReservationPage> GetUpcomingReservationsAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _reservations.GetUpcomingReservationsAsync(count, offset, cancellationToken);

    /// <summary>设置或取消剧集预约。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="active">true 表示预约，false 表示取消预约。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetReservationAsync(
        string seriesId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _reservations.SetReservationAsync(seriesId, active, cancellationToken);

    /// <summary>取消剧集预约。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteReservationAsync(
        string seriesId,
        CancellationToken cancellationToken = default
    ) => _reservations.SetReservationAsync(seriesId, active: false, cancellationToken);

    /// <summary>获取首页剧场和推荐分组。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按服务端分组返回的剧集。</returns>
    public Task<FeedPage> GetFeedCategoriesAsync(CancellationToken cancellationToken = default) =>
        _discovery.GetFeedCategoriesAsync(cancellationToken);

    /// <summary>创建剧集分集分享链接。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端返回的分享 URL 和关联 ID。</returns>
    /// <remarks>部分节点暂未开放短链接生成；这种情况下仍返回 SeriesId 和 VideoId，Url 与 ShortUrl 为空。</remarks>
    public Task<ShareLink> CreateShareLinkAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _discovery.CreateShareLinkAsync(seriesId, videoId, cancellationToken);

    /// <summary>从分享 URL 中解析剧集和分集 ID。</summary>
    /// <param name="url">完整分享 URL。</param>
    /// <returns>解析出的分享目标；链接不含某个 ID 时对应属性为空字符串。</returns>
    public ShareLink ResolveShareLink(string url) => DiscoveryService.ResolveShareLink(url);

    /// <summary>收藏或取消收藏一部剧。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="videoId">用于定位收藏记录的分集视频 ID。</param>
    /// <param name="active">true 表示收藏，false 表示取消收藏。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetFavoriteAsync(
        string seriesId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _favorites.SetFavoriteAsync(seriesId, videoId, active, cancellationToken);

    /// <summary>关注或取消关注一个账号。</summary>
    /// <param name="userId">账号 ID。</param>
    /// <param name="active">true 表示关注，false 表示取消关注。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>该写操作需要登录。</remarks>
    public Task<PhoenixOperationResult> SetFollowAsync(
        string userId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _follows.SetFollowAsync(userId, active, cancellationToken);

    /// <summary>点赞或取消点赞一部剧的一集。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="videoId">用于定位剧集的分集视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetDramaEpisodeLikeAsync(
        string seriesId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _likes.SetDramaEpisodeLikeAsync(seriesId, videoId, active, cancellationToken);
}
