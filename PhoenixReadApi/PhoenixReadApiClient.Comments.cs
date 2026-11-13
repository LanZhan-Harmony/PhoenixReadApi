namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>
    /// 获取指定分集在指定时间窗口内的弹幕。
    /// </summary>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="startMs">时间窗口起点（毫秒）。</param>
    /// <param name="durationMs">时间窗口长度（毫秒），不是结束时间。</param>
    /// <param name="cancellationToken">取消请求的令牌。</param>
    /// <returns>
    /// 自动合并、去重并按时间排序后的弹幕页。Total 是服务端报告的数量，Items.Count 是实际返回的记录数，
    /// 两者可能不同。
    /// </returns>
    /// <remarks>
    /// 客户端会根据服务端游标自动请求多个时间段，并对“有统计数量但返回空列表”的响应进行有限重试。
    /// durationMs 必须为正数；例如从视频开头读取十分钟可传 0 和 600000。
    /// </remarks>
    public Task<DanmakuPage> GetDanmakuAsync(
        string seriesId,
        string videoId,
        long startMs,
        long durationMs,
        CancellationToken cancellationToken = default
    ) => _danmaku.GetAsync(seriesId, videoId, startMs, durationMs, cancellationToken);

    /// <summary>获取一页剧评。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="count">本页最多返回的评论数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="sort">排序方式：默认为 <see cref="CommentSortOrder.Hot"/>（最热）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含评论、总数、下一页游标和 HasMore 状态的 <see cref="CommentPage"/>。</returns>
    /// <remarks>
    /// 当 HasMore 为 true 时，将返回页的 Cursor 原样传给下一次调用；不要自行解析或修改游标。
    /// </remarks>
    public Task<CommentPage> GetDramaReviewsAsync(
        string seriesId,
        int count = 10,
        string cursor = "",
        CommentSortOrder sort = CommentSortOrder.Hot,
        CancellationToken cancellationToken = default
    ) => _comments.GetDramaReviewsAsync(seriesId, count, cursor, cancellationToken, sort);

    /// <summary>获取指定分集的一页评论。</summary>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="count">本页最多返回的评论数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="sort">排序方式：默认为 <see cref="CommentSortOrder.Hot"/>（最热）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含评论、作者、总数、下一页游标和 HasMore 状态的 <see cref="CommentPage"/>。</returns>
    /// <remarks>当 HasMore 为 true 时，将 Cursor 原样传给下一次调用。</remarks>
    public Task<CommentPage> GetEpisodeCommentsAsync(
        string seriesId,
        string videoId,
        int count = 10,
        string cursor = "",
        CommentSortOrder sort = CommentSortOrder.Hot,
        CancellationToken cancellationToken = default
    ) =>
        _comments.GetEpisodeCommentsAsync(
            seriesId,
            videoId,
            count,
            cursor,
            cancellationToken,
            sort
        );

    /// <summary>获取一条评论下的回复。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="count">本页最多返回的回复数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>回复、总数、下一页游标和 HasMore 状态。</returns>
    public Task<CommentReplyPage> GetCommentRepliesAsync(
        string commentId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) =>
        _comments.GetRepliesAsync(
            commentId,
            string.Empty,
            string.Empty,
            count,
            cursor,
            cancellationToken
        );

    /// <summary>获取一条评论下的回复（带所属剧集或视频 ID）。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="groupId">所属剧集 ID 或视频 ID。</param>
    /// <param name="count">本页最多返回的回复数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>回复、总数、下一页游标和 HasMore 状态。</returns>
    public Task<CommentReplyPage> GetCommentRepliesAsync(
        string commentId,
        string groupId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) =>
        _comments.GetRepliesAsync(
            commentId,
            groupId,
            string.Empty,
            count,
            cursor,
            cancellationToken
        );

    /// <summary>获取一条分集评论下的回复（同时指定剧集 ID 与分集视频 ID）。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="count">本页最多返回的回复数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>回复、总数、下一页游标和 HasMore 状态。</returns>
    public Task<CommentReplyPage> GetCommentRepliesAsync(
        string commentId,
        string seriesId,
        string videoId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _comments.GetRepliesAsync(commentId, seriesId, videoId, count, cursor, cancellationToken);

    /// <summary>获取一条剧评下的回复。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="count">本页最多返回的回复数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>回复、总数、下一页游标和 HasMore 状态。</returns>
    public Task<CommentReplyPage> GetDramaReviewRepliesAsync(
        string commentId,
        string seriesId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) =>
        _comments.GetDramaReviewRepliesAsync(commentId, seriesId, count, cursor, cancellationToken);

    /// <summary>获取一条分集评论下的回复。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="count">本页最多返回的回复数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>回复、总数、下一页游标和 HasMore 状态。</returns>
    public Task<CommentReplyPage> GetEpisodeCommentRepliesAsync(
        string commentId,
        string seriesId,
        string videoId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) =>
        _comments.GetEpisodeCommentRepliesAsync(
            commentId,
            seriesId,
            videoId,
            count,
            cursor,
            cancellationToken
        );

    /// <summary>发送一条分集评论。</summary>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="text">评论文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>发送评论需要登录；服务端还可能进行内容审核。</remarks>
    public Task<PhoenixOperationResult> SendEpisodeCommentAsync(
        string seriesId,
        string videoId,
        string text,
        CancellationToken cancellationToken = default
    ) => _comments.SendCommentAsync(seriesId, videoId, text, cancellationToken);

    /// <summary>发送一条五分剧评（兼容重载）。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="text">剧评文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>发送剧评需要登录；新代码建议使用带 rating 的重载。</remarks>
    public Task<PhoenixOperationResult> SendDramaReviewAsync(
        string seriesId,
        string text,
        CancellationToken cancellationToken = default
    ) => _comments.SendReviewAsync(seriesId, 5.0, text, cancellationToken);

    /// <summary>按 0~5 星评分发送一条剧评。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="rating">用户评分，范围为 0.5 到 5.0，必须是 0.5 的倍数。</param>
    /// <param name="text">剧评文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>红果协议中的 score 会自动按 0.5 星映射为 0 到 9。</remarks>
    public Task<PhoenixOperationResult> SendDramaReviewAsync(
        string seriesId,
        double rating,
        string text,
        CancellationToken cancellationToken = default
    ) => _comments.SendReviewAsync(seriesId, rating, text, cancellationToken);

    /// <summary>发送一条弹幕。</summary>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="text">弹幕文本。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>发送弹幕需要登录。当前接口不接受时间位置，服务端会按请求上下文处理。</remarks>
    public Task<PhoenixOperationResult> SendDanmakuAsync(
        string seriesId,
        string videoId,
        string text,
        CancellationToken cancellationToken = default
    ) => _comments.SendDanmakuAsync(seriesId, videoId, text, cancellationToken);

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
    ) => _comments.LikeCommentAsync(commentId, groupId, active, cancellationToken);

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
    ) => _comments.DeleteCommentAsync(commentId, groupId, cancellationToken);

    /// <summary>回复一条分集评论。</summary>
    /// <param name="commentId">目标评论 ID。</param>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="text">回复文本，长度为 1 到 4000 个字符。</param>
    /// <param name="replyToUserId">要回复的用户 ID；没有指定时传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>回复需要登录；服务端会校验目标评论和内容。</remarks>
    public Task<PhoenixOperationResult> SendEpisodeCommentReplyAsync(
        string commentId,
        string seriesId,
        string videoId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    ) =>
        _comments.SendReplyAsync(
            commentId,
            seriesId,
            videoId,
            text,
            replyToUserId,
            cancellationToken
        );

    /// <summary>回复一条整剧剧评。</summary>
    /// <param name="commentId">目标剧评 ID。</param>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="text">回复文本，长度为 1 到 4000 个字符。</param>
    /// <param name="replyToUserId">要回复的用户 ID；没有指定时传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>回复需要登录；服务端会校验目标评论和内容。</remarks>
    public Task<PhoenixOperationResult> SendDramaReviewReplyAsync(
        string commentId,
        string seriesId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    ) =>
        _comments.SendDramaReviewReplyAsync(
            commentId,
            seriesId,
            text,
            replyToUserId,
            cancellationToken
        );

    /// <summary>点赞或取消点赞剧评。</summary>
    /// <param name="commentId">剧评评论 ID。</param>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetReviewLikeAsync(
        string commentId,
        string seriesId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) =>
        _comments.LikeCommentAsync(
            commentId,
            seriesId,
            active,
            cancellationToken,
            objectType: 2,
            commentType: 4
        );

    /// <summary>点赞或取消点赞弹幕。</summary>
    /// <param name="commentId">弹幕 ID。</param>
    /// <param name="videoId">视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetDanmakuLikeAsync(
        string commentId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) =>
        _comments.LikeCommentAsync(
            commentId,
            videoId,
            active,
            cancellationToken,
            objectType: 2,
            commentType: 20
        );

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
        _comments.LikeCommentAsync(
            replyId,
            groupId,
            active,
            cancellationToken,
            objectType: 8,
            commentType: 4
        );

    /// <summary>删除一条回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteReplyAsync(
        string replyId,
        CancellationToken cancellationToken = default
    ) => _comments.DeleteReplyAsync(replyId, cancellationToken);

    /// <summary>删除一条剧评。</summary>
    /// <param name="commentId">剧评 ID。</param>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteDramaReviewAsync(
        string commentId,
        string seriesId,
        CancellationToken cancellationToken = default
    ) =>
        _comments.DeleteCommentAsync(
            commentId,
            seriesId,
            cancellationToken,
            dataType: 4,
            serviceId: 2,
            groupType: 1
        );

    /// <summary>删除一条弹幕。</summary>
    /// <param name="commentId">弹幕 ID。</param>
    /// <param name="videoId">视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteDanmakuAsync(
        string commentId,
        string videoId,
        CancellationToken cancellationToken = default
    ) =>
        _comments.DeleteCommentAsync(
            commentId,
            videoId,
            cancellationToken,
            dataType: 4,
            serviceId: 20,
            groupType: 30
        );

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
    ) => _emoticons.GetEmoticonsAsync(isCollected, offset, count, cancellationToken);

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
    ) => _emoticons.CollectEmoticonAsync(imageId, isAdd, imageUrl, imageWidth, imageHeight, imageType, cancellationToken);

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
    ) => _emoticons.UploadPictureAsync(imageStream, fileName, contentType, cancellationToken);

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
    ) => _emoticons.UploadPictureAsync(imageBytes, fileName, contentType, cancellationToken);
}
