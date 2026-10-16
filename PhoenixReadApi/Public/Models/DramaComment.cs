namespace PhoenixReadApi;

/// <summary>一条剧评或分集评论。</summary>
public sealed class DramaComment
{
    /// <summary>评论 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>评论文本。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>评论作者。</summary>
    public User Author { get; init; } = new();

    /// <summary>创建时间的 Unix 时间戳（秒）。</summary>
    public long CreatedAtUnixSeconds { get; init; }

    /// <summary>评论在视频中的位置（毫秒）；剧评为 0。</summary>
    public long OffsetMs { get; init; }

    /// <summary>获赞数。</summary>
    public long LikeCount { get; init; }

    /// <summary>回复数。</summary>
    public long ReplyCount { get; init; }
}
