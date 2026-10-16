namespace PhoenixReadApi;

/// <summary>一条弹幕。</summary>
public sealed class DanmakuItem
{
    /// <summary>弹幕 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>评论文本。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>评论时间戳（毫秒）。</summary>
    public long TimeMs { get; init; }
}

