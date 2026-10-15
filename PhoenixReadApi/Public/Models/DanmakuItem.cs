namespace PhoenixReadApi;

/// <summary>一条弹幕。</summary>
public class DanmakuItem
{
    /// <summary>红果接口返回的弹幕 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>评论文本。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>评论时间戳（毫秒）。</summary>
    public long TimeMs { get; init; }
}

