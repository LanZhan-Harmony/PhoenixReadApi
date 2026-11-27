namespace PhoenixReadApi;

/// <summary>评论或回复操作的目标定位信息。</summary>
public readonly record struct CommentTarget
{
    /// <summary>目标内容类型。</summary>
    public CommentTargetKind Kind { get; }

    /// <summary>主对象 ID（剧评或分集时为剧集 ID；短视频时为视频 ID；图文时为帖子 ID）。</summary>
    public string PrimaryId { get; }

    /// <summary>次级视频 ID（仅短剧分集时需要）。</summary>
    public string SecondaryId { get; }

    private CommentTarget(CommentTargetKind kind, string primaryId, string secondaryId = "")
    {
        Kind = kind;
        PrimaryId = primaryId;
        SecondaryId = secondaryId;
    }

    /// <summary>创建指向整剧剧评的评论目标。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    public static CommentTarget FromDramaReview(string dramaId) =>
        new(CommentTargetKind.DramaReview, dramaId);

    /// <summary>创建指向短剧分集的评论目标。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    public static CommentTarget FromEpisode(string dramaId, string videoId) =>
        new(CommentTargetKind.Episode, dramaId, videoId);

    /// <summary>创建指向独立短视频或视频合集分集的评论目标。</summary>
    /// <param name="videoId">短视频或合集分集视频 ID。</param>
    public static CommentTarget FromShortVideo(string videoId) =>
        new(CommentTargetKind.ShortVideo, videoId);

    /// <summary>创建指向图文帖子的评论目标。</summary>
    /// <param name="postId">图文帖子 ID。</param>
    public static CommentTarget FromPost(string postId) => new(CommentTargetKind.Post, postId);
}
