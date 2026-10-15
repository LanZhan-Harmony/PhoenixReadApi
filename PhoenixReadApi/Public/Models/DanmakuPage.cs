namespace PhoenixReadApi;

/// <summary>A bounded danmaku time window.</summary>
public class DanmakuPage
{
    /// <summary>Episode identifier.</summary>
    public string EpisodeId { get; init; } = string.Empty;

    /// <summary>Comments in the requested time window.</summary>
    public IReadOnlyList<DanmakuItem> Items { get; init; } = [];

    /// <summary>Start of the requested window in milliseconds.</summary>
    public long StartMs { get; init; }

    /// <summary>Next window position in milliseconds.</summary>
    public long NextMs { get; init; }

    /// <summary>Total comments reported by the provider.</summary>
    public long Total { get; init; }
}

