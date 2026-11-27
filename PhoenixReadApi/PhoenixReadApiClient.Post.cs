namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取指定图文帖子的完整正文、高清多图与关联短剧。</summary>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>图文帖子的详细资料实体。</returns>
    public Task<Post> GetPostDetailsAsync(
        string postId,
        CancellationToken cancellationToken = default
    ) => _circle.GetCirclePostAsync(postId, cancellationToken);
}
