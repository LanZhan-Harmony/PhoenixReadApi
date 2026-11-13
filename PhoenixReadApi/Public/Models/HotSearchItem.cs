namespace PhoenixReadApi;

/// <summary>热搜条目。</summary>
public sealed class HotSearchItem
{
    /// <summary>短剧 ID 或话题 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>短剧标题或话题名称。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>封面图片 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>分集总数。</summary>
    public int EpisodeCount { get; init; }

    /// <summary>热搜值或卡片描述（如“389万热搜值”）。</summary>
    public string CardTips { get; init; } = string.Empty;

    /// <summary>热度文案（如“4011万热度”）。</summary>
    public string Heat { get; init; } = string.Empty;

    /// <summary>角标标签（如“新剧”）。</summary>
    public string Tag { get; init; } = string.Empty;

    /// <summary>跳转或动作 URL（如话题链接）。</summary>
    public string Url { get; init; } = string.Empty;
}
