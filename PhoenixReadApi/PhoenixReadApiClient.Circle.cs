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

    /// <summary>获取指定红果圈图文帖子的完整正文、高清多图与关联短剧。</summary>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>红果圈图文帖子的详细资料实体。</returns>
    public Task<Post> GetCirclePostAsync(
        string postId,
        CancellationToken cancellationToken = default
    ) => _circle.GetCirclePostAsync(postId, cancellationToken);

    /// <summary>获取指定红果圈短视频的完整元数据、演员阵容与关联短剧。</summary>
    /// <param name="videoId">短视频 ID，必须是数字字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>红果圈短视频的详细资料实体。</returns>
    public Task<ShortVideo> GetCircleVideoAsync(
        string videoId,
        CancellationToken cancellationToken = default
    ) => _circle.GetCircleVideoAsync(videoId, cancellationToken);
}
