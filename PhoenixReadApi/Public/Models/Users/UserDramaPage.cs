namespace PhoenixReadApi;

/// <summary>账号主页中的一页剧集记录。</summary>
public sealed class UserDramaPage
{
    /// <summary>本页剧集。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>基于游标的下一页标记。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>基于偏移量的下一页起点。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }

    /// <summary>服务端报告的总数量。</summary>
    public long Total { get; init; }
}
