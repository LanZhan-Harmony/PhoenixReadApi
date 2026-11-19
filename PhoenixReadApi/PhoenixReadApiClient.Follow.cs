namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
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
    ) => _follow.SetFollowAsync(userId, active, cancellationToken);

    /// <summary>获取账号粉丝列表的一页（如果用户设置了隐藏粉丝列表，则返回受隐私保护且列表为空）。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50，默认为 20。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>粉丝列表及 Cursor、HasMore 和 IsPrivacyProtected 状态。</returns>
    public Task<UserRelationPage> GetUserFollowersAsync(
        string userId,
        string cursor = "",
        int count = 20,
        CancellationToken cancellationToken = default
    ) => _follow.GetFollowersAsync(userId, cursor, count, cancellationToken);

    /// <summary>获取账号关注人员列表的一页（如果用户设置了隐藏关注列表，则返回受隐私保护且列表为空）。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50，默认为 20。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>关注人员列表及 Cursor、HasMore 和 IsPrivacyProtected 状态。</returns>
    public Task<UserRelationPage> GetUserFollowingsAsync(
        string userId,
        string cursor = "",
        int count = 20,
        CancellationToken cancellationToken = default
    ) => _follow.GetFollowingsAsync(userId, cursor, count, cancellationToken);
}
