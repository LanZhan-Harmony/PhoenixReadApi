namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取一页短剧剧评。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="count">本页最多返回的评论数，范围为 1 到 90。</param>
    /// <param name="sort">排序方式：默认为 <see cref="CommentSortOrder.Hot"/>（最热）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含剧评列表、全剧公信评分、打分人数、AI综评、总数、下一页游标和 HasMore 状态的 <see cref="DramaReviewPage"/>。</returns>
    public Task<DramaReviewPage> GetDramaReviewsAsync(
        string dramaId,
        string cursor = "",
        int count = 10,
        CommentSortOrder sort = CommentSortOrder.Hot,
        CancellationToken cancellationToken = default
    ) => _comment.GetDramaReviewsAsync(dramaId, cursor, count, cancellationToken, sort);

    /// <summary>按 0~5 星评分发送一条剧评。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="rating">用户评分，范围为 0.5 到 5.0，必须是 0.5 的倍数。</param>
    /// <param name="text">剧评文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果，成功时在 <see cref="PhoenixOperationResult.Id"/> 中返回剧评 ID。</returns>
    /// <remarks>红果协议中的 score 会自动按 0.5 星映射为 0 到 9。</remarks>
    public Task<PhoenixOperationResult> SendDramaReviewAsync(
        string dramaId,
        double rating,
        string text,
        CancellationToken cancellationToken = default
    ) => _comment.SendReviewAsync(dramaId, rating, text, cancellationToken);

    /// <summary>获取指定目标（短剧分集、短视频或图文帖子）的一页评论。</summary>
    /// <param name="target">评论目标（可通过 <see cref="CommentTarget"/> 工厂方法构造）。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="count">本页最多返回的评论数，范围为 1 到 90。</param>
    /// <param name="sort">排序方式：默认为 <see cref="CommentSortOrder.Hot"/>（最热）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含评论、作者、总数、下一页游标和 HasMore 状态的 <see cref="CommentPage"/>。</returns>
    public Task<CommentPage> GetCommentsAsync(
        CommentTarget target,
        string cursor = "",
        int count = 20,
        CommentSortOrder sort = CommentSortOrder.Hot,
        CancellationToken cancellationToken = default
    ) => _comment.GetCommentsAsync(target, cursor, count, sort, cancellationToken);

    /// <summary>获取指定评论或剧评下的回复列表。</summary>
    /// <param name="target">目标定位信息（支持剧评、短剧分集、短视频或图文帖子）。</param>
    /// <param name="commentId">目标评论或剧评 ID。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="count">本页最多返回的回复数，范围为 1 到 90。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>回复、总数、下一页游标和 HasMore 状态。</returns>
    public Task<CommentReplyPage> GetCommentRepliesAsync(
        CommentTarget target,
        string commentId,
        string cursor = "",
        int count = 20,
        CancellationToken cancellationToken = default
    ) => _comment.GetCommentRepliesAsync(target, commentId, cursor, count, cancellationToken);

    /// <summary>为指定目标（短剧分集、短视频或图文帖子）发送一条评论。</summary>
    /// <param name="target">评论目标。</param>
    /// <param name="text">评论文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果，成功时在 <see cref="PhoenixOperationResult.Id"/> 中返回评论 ID。</returns>
    public Task<PhoenixOperationResult> SendCommentAsync(
        CommentTarget target,
        string text,
        CancellationToken cancellationToken = default
    ) => _comment.SendCommentAsync(target, text, cancellationToken);

    /// <summary>回复一条剧评或评论。</summary>
    /// <param name="target">目标定位信息。</param>
    /// <param name="commentId">目标评论或剧评 ID。</param>
    /// <param name="text">回复文本，长度为 1 到 4000 个字符。</param>
    /// <param name="replyToUserId">要回复的用户 ID；没有指定时传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果，成功时在 <see cref="PhoenixOperationResult.Id"/> 中返回回复 ID。</returns>
    public Task<PhoenixOperationResult> SendCommentReplyAsync(
        CommentTarget target,
        string commentId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    ) => _comment.SendCommentReplyAsync(target, commentId, text, replyToUserId, cancellationToken);

    /// <summary>点赞或取消点赞一条剧评或评论。</summary>
    /// <param name="target">目标定位信息。</param>
    /// <param name="commentId">评论或剧评 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetCommentLikeAsync(
        CommentTarget target,
        string commentId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _comment.SetCommentLikeAsync(target, commentId, active, cancellationToken);

    /// <summary>点赞或取消点赞一条回复。</summary>
    /// <param name="target">目标定位信息。</param>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetReplyLikeAsync(
        CommentTarget target,
        string replyId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _comment.SetReplyLikeAsync(target, replyId, active, cancellationToken);

    /// <summary>删除自己发布的剧评或评论。</summary>
    /// <param name="target">目标定位信息。</param>
    /// <param name="commentId">评论或剧评 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteCommentAsync(
        CommentTarget target,
        string commentId,
        CancellationToken cancellationToken = default
    ) => _comment.DeleteCommentAsync(target, commentId, cancellationToken);

    /// <summary>删除自己发布的一条回复。</summary>
    /// <param name="target">目标定位信息。</param>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteReplyAsync(
        CommentTarget target,
        string replyId,
        CancellationToken cancellationToken = default
    ) => _comment.DeleteReplyAsync(target, replyId, cancellationToken);
}
