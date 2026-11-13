namespace PhoenixReadApi;

/// <summary>通知中心的一条通知消息（包含剧集更新、预约上线等提醒）。</summary>
public sealed class NoticeItem
{
    /// <summary>通知唯一标识符。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>通知消息类型。</summary>
    public NoticeType Type { get; init; } = NoticeType.Unknown;

    /// <summary>关联的短剧 ID。</summary>
    public string DramaId { get; init; } = string.Empty;

    /// <summary>通知标题或剧集名称。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>封面图片 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>起始分集序号（例如追更从第 20 集更新到 30 集）。</summary>
    public int FromEpisode { get; init; }

    /// <summary>当前剧集总集数。</summary>
    public int EpisodeCount { get; init; }

    /// <summary>通知产生的时间戳（Unix 毫秒）。</summary>
    public long CreatedAtUnixMilliseconds { get; init; }

    /// <summary>该条通知是否已被标记为已读。</summary>
    public bool IsRead { get; init; }
}
