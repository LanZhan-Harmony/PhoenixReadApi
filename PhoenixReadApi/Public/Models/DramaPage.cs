namespace PhoenixReadApi;

/// <summary>带分页状态的剧集列表。</summary>
public sealed class DramaPage
{
    /// <summary>当前页的剧集。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>服务端报告的总数。</summary>
    public long Total { get; init; }

    /// <summary>下一次偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }
}
