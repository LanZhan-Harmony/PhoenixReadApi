namespace PhoenixReadApi;

/// <summary>A single danmaku item.</summary>
public class DanmakuItem
{
    /// <summary>Provider comment identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Comment text.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Comment timestamp in milliseconds.</summary>
    public long TimeMs { get; init; }
}

