namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取当前客户端账号点赞的剧集。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>点赞剧集和分页状态。</returns>
    public Task<DramaPage> GetDramaLikesAsync(
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

    /// <summary>点赞或取消点赞独立短视频或合集分集视频。</summary>
    /// <param name="videoId">短视频或合集分集视频 ID，必须是数字字符串。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetShortVideoLikeAsync(
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _like.SetShortVideoLikeAsync(videoId, active, cancellationToken);

    /// <summary>点赞或取消点赞一篇图文帖子。</summary>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetPostLikeAsync(
        string postId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _like.SetPostLikeAsync(postId, active, cancellationToken);
}
