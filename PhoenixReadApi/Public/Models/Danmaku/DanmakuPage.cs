namespace PhoenixReadApi;

/// <summary>指定时间范围内的弹幕。</summary>
public sealed class DanmakuPage
{
    /// <summary>分集 ID。</summary>
    public string EpisodeId { get; init; } = string.Empty;

    /// <summary>请求时间范围内的弹幕。</summary>
    public IReadOnlyList<DanmakuItem> Items { get; init; } = [];

    /// <summary>请求时间范围的起点（毫秒）。</summary>
    public long StartMs { get; init; }

    /// <summary>已读取范围的下一时间点（毫秒）。</summary>
    public long NextMs { get; init; }

    /// <summary>接口报告的时间范围内弹幕总数。</summary>
    public long Total { get; init; }
}
