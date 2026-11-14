namespace PhoenixReadApi;

/// <summary>预约列表分页结果。</summary>
public sealed class ReservationPage
{
    /// <summary>当前页预约记录。</summary>
    public IReadOnlyList<ReservationItem> Items { get; init; } = [];

    /// <summary>下一次偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }

    /// <summary>服务端报告的总数。</summary>
    public long Total { get; init; }
}
