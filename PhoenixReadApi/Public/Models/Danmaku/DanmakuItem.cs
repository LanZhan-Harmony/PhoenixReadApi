namespace PhoenixReadApi;

/// <summary>一条弹幕。</summary>
public sealed class DanmakuItem
{
    /// <summary>弹幕唯一 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>弹幕文本内容。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>弹幕在视频中出现的播放时间偏移（毫秒）。</summary>
    public long OffsetMs { get; init; }
}
