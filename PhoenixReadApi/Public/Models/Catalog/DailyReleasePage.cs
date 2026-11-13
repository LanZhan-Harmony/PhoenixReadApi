namespace PhoenixReadApi;

/// <summary>按指定日期查询的上新日历短剧结果页。</summary>
public sealed class DailyReleasePage
{
    /// <summary>当前查询的目标日期。</summary>
    public DateOnly Date { get; init; }

    /// <summary>服务端日历目前支持的所有可选日期列表。</summary>
    public IReadOnlyList<DateOnly> AvailableDates { get; init; } = [];

    /// <summary>当天上新及排期预告短剧条目列表。</summary>
    public IReadOnlyList<DailyReleaseItem> Items { get; init; } = [];

    /// <summary>下一页分页偏移量游标。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有更多短剧。</summary>
    public bool HasMore { get; init; }

    /// <summary>服务端本次日历查询会话标识。</summary>
    public string SessionId { get; init; } = string.Empty;
}
