namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取任意公开账号的主页及首批公开内容。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>
    /// 账号身份、首批收藏、点赞、动态和关联剧集，以及各内容集合的分页状态。
    /// </returns>
    /// <remarks>
    /// 该方法适合一次加载主页摘要。需要继续读取时，使用返回对象中的分页状态调用对应的
    /// GetUserActivitiesAsync、GetUserRelatedDramasAsync、GetUserCollectionsAsync 或 GetUserLikesAsync。
    /// </remarks>
    public Task<UserProfile> GetUserProfileAsync(
        string userId,
        CancellationToken cancellationToken = default
    ) => _users.GetAsync(userId, cancellationToken);

    /// <summary>根据剧集署名关系获取对应账号的主页。</summary>
    /// <param name="credit">详情中的剧集署名关系；方法会使用其中的 User ID 和已有资料。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>账号主页及首批公开内容和分页状态。</returns>
    /// <remarks>使用此重载可以保留详情中已经取得的账号名称、简介和头像。</remarks>
    public Task<UserProfile> GetUserProfileAsync(
        DramaCredit credit,
        CancellationToken cancellationToken = default
    ) => _users.GetAsync(credit, cancellationToken);

    /// <summary>获取账号关联剧集的一页。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>关联剧集及 Cursor、HasMore 和 Total 分页信息。</returns>
    /// <remarks>当 HasMore 为 true 时，将 Cursor 原样传给下一次调用。</remarks>
    public Task<UserDramaPage> GetUserRelatedDramasAsync(
        string userId,
        int count = 18,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _users.GetRelatedDramasAsync(userId, count, cursor, cancellationToken);

    /// <summary>获取账号动态的一页。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>动态记录及 Cursor、HasMore 和 Total 分页信息。</returns>
    /// <remarks>当 HasMore 为 true 时，将 Cursor 原样传给下一次调用。</remarks>
    public Task<UserActivityPage> GetUserActivitiesAsync(
        string userId,
        int count = 18,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _users.GetActivitiesAsync(userId, count, cursor, cancellationToken);

    /// <summary>获取账号收藏剧集的一页。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">分页偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏剧集及 NextOffset、HasMore 和 Total 分页信息。</returns>
    /// <remarks>当 HasMore 为 true 时，将 NextOffset 原样传给下一次调用。</remarks>
    public Task<UserDramaPage> GetUserCollectionsAsync(
        string userId,
        int count = 18,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _users.GetCollectionsAsync(userId, count, offset, cancellationToken);

    /// <summary>获取账号点赞剧集的一页。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">分页偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>点赞剧集及 NextOffset、HasMore 和 Total 分页信息。</returns>
    /// <remarks>当 HasMore 为 true 时，将 NextOffset 原样传给下一次调用。</remarks>
    public Task<UserDramaPage> GetUserLikesAsync(
        string userId,
        int count = 18,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _users.GetLikesAsync(userId, count, offset, cancellationToken);
}
