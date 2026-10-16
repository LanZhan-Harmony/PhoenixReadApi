namespace PhoenixReadApi;

/// <summary>推荐接口返回的一页结果。</summary>
public sealed class RecommendationPage
{
    /// <summary>推荐条目。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>下一页的分页偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>分页会话标识。</summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }
}

