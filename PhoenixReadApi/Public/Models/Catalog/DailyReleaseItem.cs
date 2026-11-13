namespace PhoenixReadApi;

/// <summary>上新日历条目（包含剧集元数据、是否上线、排期时间与上线说明）。</summary>
public sealed class DailyReleaseItem
{
    /// <summary>短剧信息。</summary>
    public Drama Drama { get; init; } = new();

    /// <summary>该剧当前是否已正式上线。</summary>
    public bool IsOnline { get; init; }

    /// <summary>排期发布上线时间（UTC 时间），若未排期或已上线则可能为空。</summary>
    public DateTimeOffset? PublishTime { get; init; }

    /// <summary>上线说明或副标题备注（例如“今日 12:00 上线”）。</summary>
    public string Note { get; init; } = string.Empty;

    /// <summary>题材分类标签。</summary>
    public IReadOnlyList<string> Categories { get; init; } = [];
}
