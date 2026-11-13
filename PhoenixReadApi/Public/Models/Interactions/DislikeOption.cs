namespace PhoenixReadApi;

/// <summary>单个不感兴趣负反馈选项。</summary>
public sealed class DislikeOption
{
    /// <summary>选项标识 ID（提交负反馈时传入）。</summary>
    public int Id { get; init; }

    /// <summary>选项正文文案（例如“不喜欢这部剧”、“剧名低俗”）。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>选项前缀或分类标头（例如“这部剧”、“演员”、“分类”）。</summary>
    public string Head { get; init; } = string.Empty;
}