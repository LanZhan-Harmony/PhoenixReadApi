namespace PhoenixReadApi;

/// <summary>指定时间范围内的弹幕。</summary>
public sealed class DanmakuPage
{
    /// <summary>分集视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>弹幕列表。</summary>
    public IReadOnlyList<DanmakuItem> Items { get; init; } = [];

    /// <summary>本次查询的起始时间点（毫秒）。</summary>
    public long StartMs { get; init; }

    /// <summary>下一次查询应使用的起始时间点（毫秒）。</summary>
    public long NextMs { get; init; }

    /// <summary>该分集内的弹幕总数量。</summary>
    public long Total { get; init; }
}
