namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>设置短剧的追更提醒状态（关注追更或取消追更）。</summary>
    /// <param name="dramaId">剧集 ID，必须是由数字构成的字符串。</param>
    /// <param name="follow">true 为开启追更提醒，false 为取消追更。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>操作是否成功。</returns>
    public Task<bool> SetFollowUpdateAsync(
        string dramaId,
        bool follow,
        CancellationToken cancellationToken = default
    ) => _notification.SetFollowUpdateAsync(dramaId, follow, cancellationToken);

    /// <summary>获取当前账号追更短剧列表的一页。</summary>
    /// <param name="cursor">上一页返回的分页游标；首次请求传空字符串。</param>
    /// <param name="count">每页请求返回数量，默认为 20，最大为 50。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含追更短剧列表与下一页游标的分页数据。</returns>
    public Task<FollowUpdatePage> GetFollowUpdatesAsync(
        string cursor = "",
        int count = 20,
        CancellationToken cancellationToken = default
    ) => _notification.GetFollowUpdatesAsync(cursor, count, cancellationToken);

    /// <summary>检查指定短剧是否已处于开启追更状态。</summary>
    /// <param name="dramaId">剧集 ID，必须是由数字构成的字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>如果已追更返回 true，否则返回 false。</returns>
    public Task<bool> IsFollowingUpdateAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    ) => _notification.IsFollowingUpdateAsync(dramaId, cancellationToken);

    /// <summary>检查并获取追更短剧的新剧集更新通知。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>自上次检查以来新产生的剧集更新通知列表。</returns>
    public Task<IReadOnlyList<NoticeItem>> CheckNoticesAsync(
        CancellationToken cancellationToken = default
    ) => _notification.CheckNoticesAsync(cancellationToken);

    /// <summary>获取通知中心当前所有的通知列表及未读统计状态。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含通知列表及未读总数的通知中心页面。</returns>
    public Task<NoticePage> GetNoticesAsync(
        CancellationToken cancellationToken = default
    ) => _notification.GetNoticesAsync(cancellationToken);

    /// <summary>批量标记指定的通知为已读状态。</summary>
    /// <param name="noticeIds">要标记为已读的通知 ID 集合。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>表示异步操作的 Task。</returns>
    public Task MarkNoticesReadAsync(
        IEnumerable<string> noticeIds,
        CancellationToken cancellationToken = default
    ) => _notification.MarkNoticesReadAsync(noticeIds, cancellationToken);

    /// <summary>清空通知中心的所有通知消息。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>表示异步操作的 Task。</returns>
    public Task ClearNoticesAsync(
        CancellationToken cancellationToken = default
    ) => _notification.ClearNoticesAsync(cancellationToken);
}
