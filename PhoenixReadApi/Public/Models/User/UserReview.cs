namespace PhoenixReadApi;

/// <summary>用户主页作品中发布的短剧评价作品。</summary>
public sealed class UserReview
{
    /// <summary>剧评 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>所评价的短剧信息。</summary>
    public Drama Drama { get; init; } = new();

    /// <summary>剧评正文内容。</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>用户打分（10分制，如 10、9）。</summary>
    public int Rating { get; init; }

    /// <summary>打分后缀文案（如 "观看2小时后点评"）。</summary>
    public string RatingText { get; init; } = string.Empty;

    /// <summary>获赞数量。</summary>
    public long LikeCount { get; init; }

    /// <summary>回复总数。</summary>
    public long ReplyCount { get; init; }

    /// <summary>发表时间戳（Unix 秒）。</summary>
    public long CreatedAtUnixSeconds { get; init; }

    /// <summary>剧评图片列表。</summary>
    public IReadOnlyList<CommentImage> Images { get; init; } = [];
}
