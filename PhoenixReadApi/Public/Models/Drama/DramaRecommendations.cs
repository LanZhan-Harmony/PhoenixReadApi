namespace PhoenixReadApi;

/// <summary>“猜你喜欢”分页结果。</summary>
public sealed class RecommendationFeedPage
{
    /// <summary>板块标题（如“猜你喜欢”）。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>服务端板块 ID，用于后续翻页请求。</summary>
    public string CellId { get; init; } = string.Empty;

    /// <summary>服务端推荐会话标识。</summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>下一页偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }

    /// <summary>当前页推荐条目（短剧或图文帖子）。</summary>
    public IReadOnlyList<RecommendationFeedItem> Items { get; init; } = [];
}
