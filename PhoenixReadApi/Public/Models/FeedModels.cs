namespace PhoenixReadApi;

/// <summary>首页或推荐页的一组剧集。</summary>
public sealed class FeedSection
{
    /// <summary>分组 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>分组标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>分组中的剧集。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];
}

/// <summary>首页或推荐页返回的分组列表。</summary>
public sealed class FeedPage
{
    /// <summary>分组列表。</summary>
    public IReadOnlyList<FeedSection> Sections { get; init; } = [];

    /// <summary>下一次偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>分页会话 ID。</summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }
}
