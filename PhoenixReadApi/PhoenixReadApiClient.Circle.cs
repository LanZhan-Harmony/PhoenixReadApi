namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取演职人员或创作者的红果圈 Feed 混合流（图文帖子与短视频）。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50，默认为 18。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>红果圈内容条目列表及 Cursor、HasMore 和 Total 分页信息。</returns>
    /// <remarks>当 HasMore 为 true 时，将 Cursor 原样传给下一次调用。</remarks>
    public Task<CirclePage> GetUserCircleAsync(
        string userId,
        string cursor = "",
        int count = 18,
        CancellationToken cancellationToken = default
    ) => _circle.GetUserCircleAsync(userId, cursor, count, cancellationToken);
}
