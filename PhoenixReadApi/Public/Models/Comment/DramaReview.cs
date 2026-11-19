namespace PhoenixReadApi;

/// <summary>短剧剧评实体。</summary>
public sealed class DramaReview
{
    /// <summary>剧评 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>剧评正文内容。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>发表剧评的用户信息。</summary>
    public User Author { get; init; } = new();

    /// <summary>发表时间戳（Unix 秒）。</summary>
    public long CreatedAtUnixSeconds { get; init; }

    /// <summary>用户为该剧打出的评分（10分制，如 10、9、8）。</summary>
    public int Rating { get; init; }

    /// <summary>评分后缀文案（例如 "观看2小时后点评"）。</summary>
    public string RatingText { get; init; } = string.Empty;

    /// <summary>获赞数量。</summary>
    public long LikeCount { get; init; }

    /// <summary>该剧评下的回复总数。</summary>
    public long ReplyCount { get; init; }

    /// <summary>剧评中包含的图片列表。</summary>
    public IReadOnlyList<CommentImage> Images { get; init; } = [];
}
