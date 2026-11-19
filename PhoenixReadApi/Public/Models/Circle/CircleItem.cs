namespace PhoenixReadApi;

/// <summary>红果圈 Feed 流中的一条内容项（短视频或图文）。</summary>
public sealed class CircleItem
{
    /// <summary>条目唯一标识（图文为 Post ID，视频为 Video ID / Series ID）。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>条目类型（短视频或图文）。</summary>
    public CircleItemType Type { get; init; }

    /// <summary>标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>正文简述或文字内容。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>封面图片 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>封面图宽度。</summary>
    public int CoverWidth { get; init; }

    /// <summary>封面图高度。</summary>
    public int CoverHeight { get; init; }

    /// <summary>发布者信息（可能为演职人员本人，也可能为粉丝或创作者）。</summary>
    public User Author { get; init; } = new();

    /// <summary>发布时间戳（Unix 秒数）。</summary>
    public long CreatedAtUnixSeconds { get; init; }

    /// <summary>点赞数。</summary>
    public long LikeCount { get; init; }

    /// <summary>评论数（对于图文）或互动回复数。</summary>
    public long CommentCount { get; init; }

    /// <summary>播放数（视频项特有）。</summary>
    public long ViewCount { get; init; }

    /// <summary>关联的红果短剧唯一 ID（若有）。</summary>
    public string DramaId { get; init; } = string.Empty;

    /// <summary>关联的红果短剧标题（若有）。</summary>
    public string DramaTitle { get; init; } = string.Empty;

    /// <summary>关联的短剧封面（若有）。</summary>
    public string DramaCoverUrl { get; init; } = string.Empty;
}
