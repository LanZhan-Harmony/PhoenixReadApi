namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取演职人员或创作者的红果圈 Feed 混合流（图文帖子与短视频）。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50，默认为 18。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>红果圈内容条目列表及 Cursor、HasMore 和 Total 分页信息。</returns>
    /// <remarks>当 HasMore 为 true 时，将 Cursor 原样传给下一次调用。</remarks>
    public Task<CirclePage> GetUserCircleAsync(
        string userId,
        int count = 18,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _circle.GetUserCircleAsync(userId, count, cursor, cancellationToken);

    /// <summary>获取指定红果圈图文帖子的完整正文、高清多图与关联短剧。</summary>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>红果圈图文帖子的详细资料实体。</returns>
    public Task<CirclePost> GetCirclePostAsync(
        string postId,
        CancellationToken cancellationToken = default
    ) => _circle.GetCirclePostAsync(postId, cancellationToken);

    /// <summary>获取指定红果圈短视频的完整元数据、演员阵容与关联短剧。</summary>
    /// <param name="videoId">短视频 ID，必须是数字字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>红果圈短视频的详细资料实体。</returns>
    public Task<CircleVideo> GetCircleVideoAsync(
        string videoId,
        CancellationToken cancellationToken = default
    ) => _circle.GetCircleVideoAsync(videoId, cancellationToken);

    /// <summary>获取指定红果圈图文帖子的评论列表。</summary>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <param name="count">每页评论数量，范围为 1 到 90，默认为 20。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>评论列表及游标信息。</returns>
    public Task<CommentPage> GetCirclePostCommentsAsync(
        string postId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _circle.GetCirclePostCommentsAsync(postId, count, cursor, cancellationToken);

    /// <summary>获取指定红果圈短视频的评论列表。</summary>
    /// <param name="videoId">短视频 ID，必须是数字字符串。</param>
    /// <param name="count">每页评论数量，范围为 1 到 90，默认为 20。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>评论列表及游标信息。</returns>
    public Task<CommentPage> GetCircleVideoCommentsAsync(
        string videoId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _circle.GetCircleVideoCommentsAsync(videoId, count, cursor, cancellationToken);

    /// <summary>获取指定红果圈图文评论下的回复列表。</summary>
    /// <param name="commentId">评论 ID，必须是数字字符串。</param>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <param name="count">每页回复数量，范围为 1 到 90，默认为 20。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含回复列表、总数、下一页游标和 HasMore 状态的 <see cref="CommentReplyPage"/>。</returns>
    public Task<CommentReplyPage> GetCirclePostCommentRepliesAsync(
        string commentId,
        string postId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _circle.GetCirclePostCommentRepliesAsync(commentId, postId, count, cursor, cancellationToken);

    /// <summary>获取指定红果圈短视频评论下的回复列表。</summary>
    /// <param name="commentId">评论 ID，必须是数字字符串。</param>
    /// <param name="videoId">短视频 ID，必须是数字字符串。</param>
    /// <param name="count">每页回复数量，范围为 1 到 90，默认为 20。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含回复列表、总数、下一页游标和 HasMore 状态的 <see cref="CommentReplyPage"/>。</returns>
    public Task<CommentReplyPage> GetCircleVideoCommentRepliesAsync(
        string commentId,
        string videoId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _circle.GetCircleVideoCommentRepliesAsync(commentId, videoId, count, cursor, cancellationToken);

    /// <summary>点赞或取消点赞红果圈图文帖子的评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="postId">图文帖子 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>写操作需要登录账号凭据。</remarks>
    public Task<PhoenixOperationResult> SetCirclePostCommentLikeAsync(
        string commentId,
        string postId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _circle.SetCirclePostCommentLikeAsync(commentId, postId, active, cancellationToken);

    /// <summary>点赞或取消点赞红果圈图文评论下的回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="postId">图文帖子 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>写操作需要登录账号凭据。</remarks>
    public Task<PhoenixOperationResult> SetCirclePostReplyLikeAsync(
        string replyId,
        string postId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _circle.SetCirclePostReplyLikeAsync(replyId, postId, active, cancellationToken);

    /// <summary>点赞或取消点赞红果圈短视频的评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="videoId">短视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>写操作需要登录账号凭据。</remarks>
    public Task<PhoenixOperationResult> SetCircleVideoCommentLikeAsync(
        string commentId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _circle.SetCircleVideoCommentLikeAsync(commentId, videoId, active, cancellationToken);

    /// <summary>点赞或取消点赞红果圈短视频评论下的回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="videoId">短视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>写操作需要登录账号凭据。</remarks>
    public Task<PhoenixOperationResult> SetCircleVideoReplyLikeAsync(
        string replyId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _circle.SetCircleVideoReplyLikeAsync(replyId, videoId, active, cancellationToken);

    /// <summary>删除自己发布的红果圈图文帖子评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="postId">图文帖子 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteCirclePostCommentAsync(
        string commentId,
        string postId,
        CancellationToken cancellationToken = default
    ) => _circle.DeleteCirclePostCommentAsync(commentId, postId, cancellationToken);

    /// <summary>删除自己发布的红果圈短视频评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="videoId">短视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteCircleVideoCommentAsync(
        string commentId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _circle.DeleteCircleVideoCommentAsync(commentId, videoId, cancellationToken);

    /// <summary>删除自己发布的圈内评论回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteCircleReplyAsync(
        string replyId,
        CancellationToken cancellationToken = default
    ) => _circle.DeleteCircleReplyAsync(replyId, cancellationToken);
}
