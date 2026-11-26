namespace PhoenixReadApi;

/// <summary>泛型阅读或观看历史分页结果。</summary>
/// <typeparam name="T">目标内容实体类型。</typeparam>
public sealed class HistoryPage<T>
{
    /// <summary>当前页记录。</summary>
    public IReadOnlyList<HistoryItem<T>> Items { get; init; } = [];

    /// <summary>服务端报告的总数。</summary>
    public long Total { get; init; }

    /// <summary>下一次偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }
}
