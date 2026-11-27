namespace PhoenixReadApi;

/// <summary>评论或回复所属的目标类型。</summary>
public enum CommentTargetKind
{
    /// <summary>整部剧集的剧评。</summary>
    DramaReview,

    /// <summary>短剧分集。</summary>
    Episode,

    /// <summary>独立短视频或视频合集分集。</summary>
    ShortVideo,

    /// <summary>图文帖子。</summary>
    Post,
}
