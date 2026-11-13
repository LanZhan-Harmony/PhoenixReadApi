namespace PhoenixReadApi;

/// <summary>通知中心通知列表及未读统计状态。</summary>
public sealed class NoticePage
{
    /// <summary>通知消息列表。</summary>
    public IReadOnlyList<NoticeItem> Items { get; init; } = [];

    /// <summary>未读通知总数。</summary>
    public int UnreadCount { get; init; }
}
