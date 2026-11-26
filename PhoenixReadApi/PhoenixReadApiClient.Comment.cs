namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取一页短剧剧评。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="count">本页最多返回的评论数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="sort">排序方式：默认为 <see cref="CommentSortOrder.Hot"/>（最热）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含剧评列表、全剧公信评分、打分人数、AI综评、总数、下一页游标和 HasMore 状态的 <see cref="DramaReviewPage"/>。</returns>
    /// <remarks>
    /// 当 HasMore 为 true 时，将返回页的 Cursor 原样传给下一次调用；不要自行解析或修改游标。
    /// </remarks>
    public Task<DramaReviewPage> GetDramaReviewsAsync(
        string dramaId,
        int count = 10,
        string cursor = "",
        CommentSortOrder sort = CommentSortOrder.Hot,
        CancellationToken cancellationToken = default
    ) => _comment.GetDramaReviewsAsync(dramaId, count, cursor, cancellationToken, sort);

    /// <summary>获取指定分集的一页评论。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="count">本页最多返回的评论数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="sort">排序方式：默认为 <see cref="CommentSortOrder.Hot"/>（最热）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含评论、作者、总数、下一页游标和 HasMore 状态的 <see cref="CommentPage"/>。</returns>
    /// <remarks>当 HasMore 为 true 时，将 Cursor 原样传给下一次调用。</remarks>
    public Task<CommentPage> GetEpisodeCommentsAsync(
        string dramaId,
        string videoId,
        int count = 10,
        string cursor = "",
        CommentSortOrder sort = CommentSortOrder.Hot,
        CancellationToken cancellationToken = default
    ) => _comment.GetEpisodeCommentsAsync(dramaId, videoId, count, cursor, cancellationToken, sort);

    /// <summary>获取一条剧评下的回复。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="count">本页最多返回的回复数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>回复、总数、下一页游标和 HasMore 状态。</returns>
    public Task<CommentReplyPage> GetDramaReviewRepliesAsync(
        string commentId,
        string dramaId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _comment.GetDramaReviewRepliesAsync(commentId, dramaId, count, cursor, cancellationToken);

    /// <summary>获取一条分集评论下的回复。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="count">本页最多返回的回复数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>回复、总数、下一页游标和 HasMore 状态。</returns>
    public Task<CommentReplyPage> GetEpisodeCommentRepliesAsync(
        string commentId,
        string dramaId,
        string videoId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) =>
        _comment.GetEpisodeCommentRepliesAsync(
            commentId,
            dramaId,
            videoId,
            count,
            cursor,
            cancellationToken
        );

    /// <summary>按 0~5 星评分发送一条剧评。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="rating">用户评分，范围为 0.5 到 5.0，必须是 0.5 的倍数。</param>
    /// <param name="text">剧评文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>红果协议中的 score 会自动按 0.5 星映射为 0 到 9。</remarks>
    public Task<PhoenixOperationResult> SendDramaReviewAsync(
        string dramaId,
        double rating,
        string text,
        CancellationToken cancellationToken = default
    ) => _comment.SendReviewAsync(dramaId, rating, text, cancellationToken);

    /// <summary>发送一条分集评论。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="text">评论文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>发送评论需要登录；服务端还可能进行内容审核。</remarks>
    public Task<PhoenixOperationResult> SendEpisodeCommentAsync(
        string dramaId,
        string videoId,
        string text,
        CancellationToken cancellationToken = default
    ) => _comment.SendCommentAsync(dramaId, videoId, text, cancellationToken);

    /// <summary>回复一条整剧剧评。</summary>
    /// <param name="commentId">目标剧评 ID。</param>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="text">回复文本，长度为 1 到 4000 个字符。</param>
    /// <param name="replyToUserId">要回复的用户 ID；没有指定时传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>回复需要登录；服务端会校验目标评论和内容。</remarks>
    public Task<PhoenixOperationResult> SendDramaReviewReplyAsync(
        string commentId,
        string dramaId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    ) =>
        _comment.SendDramaReviewReplyAsync(
            commentId,
            dramaId,
            text,
            replyToUserId,
            cancellationToken
        );

    /// <summary>回复一条分集评论。</summary>
    /// <param name="commentId">目标评论 ID。</param>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="text">回复文本，长度为 1 到 4000 个字符。</param>
    /// <param name="replyToUserId">要回复的用户 ID；没有指定时传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>回复需要登录；服务端会校验目标评论和内容。</remarks>
    public Task<PhoenixOperationResult> SendEpisodeCommentReplyAsync(
        string commentId,
        string dramaId,
        string videoId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    ) =>
        _comment.SendReplyAsync(
            commentId,
            dramaId,
            videoId,
            text,
            replyToUserId,
            cancellationToken
        );

    /// <summary>点赞或取消点赞剧评。</summary>
    /// <param name="commentId">剧评评论 ID。</param>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetDramaReviewLikeAsync(
        string commentId,
        string dramaId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) =>
        _comment.LikeCommentAsync(
            commentId,
            dramaId,
            active,
            cancellationToken,
            objectType: 2,
            commentType: 4
        );

    /// <summary>点赞或取消点赞一条评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="groupId">评论所属的剧集或视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>该写操作需要登录。</remarks>
    public Task<PhoenixOperationResult> SetCommentLikeAsync(
        string commentId,
        string groupId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _comment.LikeCommentAsync(commentId, groupId, active, cancellationToken);

    /// <summary>点赞或取消点赞一条回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="groupId">回复所属的视频或剧集 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetReplyLikeAsync(
        string replyId,
        string groupId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) =>
        _comment.LikeCommentAsync(
            replyId,
            groupId,
            active,
            cancellationToken,
            objectType: 8,
            commentType: 4
        );

    /// <summary>删除一条剧评。</summary>
    /// <param name="commentId">剧评 ID。</param>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteDramaReviewAsync(
        string commentId,
        string dramaId,
        CancellationToken cancellationToken = default
    ) =>
        _comment.DeleteCommentAsync(
            commentId,
            dramaId,
            cancellationToken,
            dataType: 4,
            serviceId: 2,
            groupType: 1
        );

    /// <summary>删除自己发布的评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="groupId">评论所属的剧集或视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>该操作需要登录，并且只能删除当前账号有权限删除的评论。</remarks>
    public Task<PhoenixOperationResult> DeleteCommentAsync(
        string commentId,
        string groupId,
        CancellationToken cancellationToken = default
    ) => _comment.DeleteCommentAsync(commentId, groupId, cancellationToken);

    /// <summary>删除一条回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteReplyAsync(
        string replyId,
        CancellationToken cancellationToken = default
    ) => _comment.DeleteReplyAsync(replyId, cancellationToken);

    /// <summary>获取指定图文帖子的评论列表。</summary>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <param name="count">每页评论数量，范围为 1 到 90，默认为 20。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>评论列表及游标信息。</returns>
    public Task<CommentPage> GetPostCommentsAsync(
        string postId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _comment.GetPostCommentsAsync(postId, count, cursor, cancellationToken);

    /// <summary>根据 <see cref="Post"/> 实体获取图文帖子的评论列表。</summary>
    /// <param name="post">图文帖子实体。</param>
    /// <param name="count">每页评论数量，范围为 1 到 90，默认为 20。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>评论列表及游标信息。</returns>
    public Task<CommentPage> GetPostCommentsAsync(
        Post post,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(post);
        return _comment.GetPostCommentsAsync(post.Id, count, cursor, cancellationToken);
    }

    /// <summary>获取指定独立短视频（红果圈、视频搜索或合集子视频）的评论列表。</summary>
    /// <param name="videoId">短视频 ID，必须是数字字符串。</param>
    /// <param name="count">每页评论数量，范围为 1 到 90，默认为 20。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>评论列表及游标信息。</returns>
    public Task<CommentPage> GetShortVideoCommentsAsync(
        string videoId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _comment.GetShortVideoCommentsAsync(videoId, count, cursor, cancellationToken);

    /// <summary>根据 <see cref="ShortVideo"/> 实体获取独立短视频的评论列表。</summary>
    /// <param name="video">独立短视频实体。</param>
    /// <param name="count">每页评论数量，范围为 1 到 90，默认为 20。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>评论列表及游标信息。</returns>
    public Task<CommentPage> GetShortVideoCommentsAsync(
        ShortVideo video,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(video);
        string videoId = !string.IsNullOrWhiteSpace(video.VideoId) ? video.VideoId : video.Id;
        return _comment.GetShortVideoCommentsAsync(videoId, count, cursor, cancellationToken);
    }

    /// <summary>获取指定图文帖子评论下的回复列表。</summary>
    /// <param name="commentId">评论 ID，必须是数字字符串。</param>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <param name="count">每页回复数量，范围为 1 到 90，默认为 20。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含回复列表、总数、下一页游标和 HasMore 状态的 <see cref="CommentReplyPage"/>。</returns>
    public Task<CommentReplyPage> GetPostCommentRepliesAsync(
        string commentId,
        string postId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _comment.GetPostCommentRepliesAsync(commentId, postId, count, cursor, cancellationToken);

    /// <summary>获取指定独立短视频评论下的回复列表。</summary>
    /// <param name="commentId">评论 ID，必须是数字字符串。</param>
    /// <param name="videoId">短视频 ID，必须是数字字符串。</param>
    /// <param name="count">每页回复数量，范围为 1 到 90，默认为 20。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含回复列表、总数、下一页游标和 HasMore 状态的 <see cref="CommentReplyPage"/>。</returns>
    public Task<CommentReplyPage> GetShortVideoCommentRepliesAsync(
        string commentId,
        string videoId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) =>
        _comment.GetShortVideoCommentRepliesAsync(
            commentId,
            videoId,
            count,
            cursor,
            cancellationToken
        );

    /// <summary>点赞或取消点赞图文帖子的评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="postId">图文帖子 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>写操作需要登录账号凭据。</remarks>
    public Task<PhoenixOperationResult> SetPostCommentLikeAsync(
        string commentId,
        string postId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _comment.LikePostCommentAsync(commentId, postId, active, cancellationToken);

    /// <summary>点赞或取消点赞图文评论下的回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="postId">图文帖子 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>写操作需要登录账号凭据。</remarks>
    public Task<PhoenixOperationResult> SetPostReplyLikeAsync(
        string replyId,
        string postId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _comment.LikePostReplyAsync(replyId, postId, active, cancellationToken);

    /// <summary>点赞或取消点赞独立短视频的评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="videoId">短视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>写操作需要登录账号凭据。</remarks>
    public Task<PhoenixOperationResult> SetShortVideoCommentLikeAsync(
        string commentId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _comment.LikeShortVideoCommentAsync(commentId, videoId, active, cancellationToken);

    /// <summary>点赞或取消点赞独立短视频评论下的回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="videoId">短视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>写操作需要登录账号凭据。</remarks>
    public Task<PhoenixOperationResult> SetShortVideoReplyLikeAsync(
        string replyId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _comment.LikeShortVideoReplyAsync(replyId, videoId, active, cancellationToken);

    /// <summary>为图文帖子发表一条评论。</summary>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <param name="text">评论文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果，成功时在 <see cref="PhoenixOperationResult.Id"/> 中返回评论 ID。</returns>
    /// <remarks>发送评论需要登录；服务端还可能进行内容审核。</remarks>
    public Task<PhoenixOperationResult> SendPostCommentAsync(
        string postId,
        string text,
        CancellationToken cancellationToken = default
    ) => _comment.SendPostCommentAsync(postId, text, cancellationToken);

    /// <summary>为 <see cref="Post"/> 图文帖子发表一条评论。</summary>
    /// <param name="post">图文帖子实体。</param>
    /// <param name="text">评论文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果，成功时在 <see cref="PhoenixOperationResult.Id"/> 中返回评论 ID。</returns>
    public Task<PhoenixOperationResult> SendPostCommentAsync(
        Post post,
        string text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(post);
        return SendPostCommentAsync(post.Id, text, cancellationToken);
    }

    /// <summary>为独立短视频或视频合集分集发表一条评论。</summary>
    /// <param name="videoId">短视频或合集分集视频 ID，必须是数字字符串。</param>
    /// <param name="text">评论文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果，成功时在 <see cref="PhoenixOperationResult.Id"/> 中返回评论 ID。</returns>
    /// <remarks>发送评论需要登录；服务端还可能进行内容审核。</remarks>
    public Task<PhoenixOperationResult> SendShortVideoCommentAsync(
        string videoId,
        string text,
        CancellationToken cancellationToken = default
    ) => _comment.SendShortVideoCommentAsync(videoId, text, cancellationToken);

    /// <summary>为 <see cref="ShortVideo"/> 短视频发表一条评论。</summary>
    /// <param name="video">短视频实体。</param>
    /// <param name="text">评论文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果，成功时在 <see cref="PhoenixOperationResult.Id"/> 中返回评论 ID。</returns>
    public Task<PhoenixOperationResult> SendShortVideoCommentAsync(
        ShortVideo video,
        string text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(video);
        return SendShortVideoCommentAsync(video.VideoId, text, cancellationToken);
    }

    /// <summary>回复一条图文帖子评论。</summary>
    /// <param name="commentId">目标评论 ID。</param>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <param name="text">回复文本，长度为 1 到 4000 个字符。</param>
    /// <param name="replyToUserId">要回复的用户 ID；没有指定时传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果，成功时在 <see cref="PhoenixOperationResult.Id"/> 中返回回复 ID。</returns>
    /// <remarks>回复需要登录；服务端会校验目标评论和内容。</remarks>
    public Task<PhoenixOperationResult> SendPostReplyAsync(
        string commentId,
        string postId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    ) => _comment.SendPostReplyAsync(commentId, postId, text, replyToUserId, cancellationToken);

    /// <summary>回复一条独立短视频或视频合集分集评论。</summary>
    /// <param name="commentId">目标评论 ID。</param>
    /// <param name="videoId">短视频或合集分集视频 ID，必须是数字字符串。</param>
    /// <param name="text">回复文本，长度为 1 到 4000 个字符。</param>
    /// <param name="replyToUserId">要回复的用户 ID；没有指定时传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果，成功时在 <see cref="PhoenixOperationResult.Id"/> 中返回回复 ID。</returns>
    /// <remarks>回复需要登录；服务端会校验目标评论和内容。</remarks>
    public Task<PhoenixOperationResult> SendShortVideoReplyAsync(
        string commentId,
        string videoId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    ) =>
        _comment.SendShortVideoReplyAsync(
            commentId,
            videoId,
            text,
            replyToUserId,
            cancellationToken
        );

    /// <summary>删除自己发布的图文帖子评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="postId">图文帖子 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeletePostCommentAsync(
        string commentId,
        string postId,
        CancellationToken cancellationToken = default
    ) => _comment.DeletePostCommentAsync(commentId, postId, cancellationToken);

    /// <summary>删除自己发布的独立短视频或视频合集分集评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="videoId">短视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteShortVideoCommentAsync(
        string commentId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _comment.DeleteShortVideoCommentAsync(commentId, videoId, cancellationToken);

    /// <summary>删除自己发布的图文帖子回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeletePostReplyAsync(
        string replyId,
        CancellationToken cancellationToken = default
    ) => _comment.DeletePostReplyAsync(replyId, cancellationToken);

    /// <summary>删除自己发布的独立短视频或视频合集分集回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteShortVideoReplyAsync(
        string replyId,
        CancellationToken cancellationToken = default
    ) => _comment.DeleteShortVideoReplyAsync(replyId, cancellationToken);

    /// <summary>获取表情包列表的一页。</summary>
    /// <param name="isCollected">true 为查询当前账号收藏的表情，false 为查询系统表情包。</param>
    /// <param name="offset">分页偏移量，首次请求传 0，后续传上一页的 NextOffset。</param>
    /// <param name="count">每页数量，默认为 20，最大为 50。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>表情包图片分页结果。</returns>
    public Task<EmoticonPage> GetEmoticonsAsync(
        bool isCollected = false,
        int offset = 0,
        int count = 20,
        CancellationToken cancellationToken = default
    ) => _emoticon.GetEmoticonsAsync(isCollected, offset, count, cancellationToken);

    /// <summary>收藏或移除表情包。</summary>
    /// <param name="imageId">表情 ID。</param>
    /// <param name="isAdd">true 为收藏添加，false 为移除收藏。</param>
    /// <param name="imageUrl">表情 URL（添加时可传入）。</param>
    /// <param name="imageWidth">表情宽度（像素）。</param>
    /// <param name="imageHeight">表情高度（像素）。</param>
    /// <param name="imageType">表情类型编码。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>操作是否成功。</returns>
    public Task<bool> CollectEmoticonAsync(
        string imageId,
        bool isAdd,
        string imageUrl = "",
        int imageWidth = 0,
        int imageHeight = 0,
        int imageType = 0,
        CancellationToken cancellationToken = default
    ) =>
        _emoticon.CollectEmoticonAsync(
            imageId,
            isAdd,
            imageUrl,
            imageWidth,
            imageHeight,
            imageType,
            cancellationToken
        );

    /// <summary>上传评论配图或剧评图片（Stream 流式上传）。</summary>
    /// <param name="imageStream">包含图片二进制数据的流对象。</param>
    /// <param name="fileName">图片文件名，默认为 "image.png"。</param>
    /// <param name="contentType">图片 MIME 类型，默认为 "image/png"。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>上传成功后返回的图片元数据（包含站内 URI 和公网 URL）。</returns>
    public Task<UploadedImage> UploadPictureAsync(
        Stream imageStream,
        string fileName = "image.png",
        string contentType = "image/png",
        CancellationToken cancellationToken = default
    ) => _emoticon.UploadPictureAsync(imageStream, fileName, contentType, cancellationToken);

    /// <summary>上传评论配图或剧评图片（byte 数组上传）。</summary>
    /// <param name="imageBytes">图片字节数组。</param>
    /// <param name="fileName">图片文件名，默认为 "image.png"。</param>
    /// <param name="contentType">图片 MIME 类型，默认为 "image/png"。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>上传成功后返回的图片元数据（包含站内 URI 和公网 URL）。</returns>
    public Task<UploadedImage> UploadPictureAsync(
        byte[] imageBytes,
        string fileName = "image.png",
        string contentType = "image/png",
        CancellationToken cancellationToken = default
    ) => _emoticon.UploadPictureAsync(imageBytes, fileName, contentType, cancellationToken);
}
