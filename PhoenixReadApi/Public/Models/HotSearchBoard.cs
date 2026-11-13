namespace PhoenixReadApi;

/// <summary>热搜榜单分类。</summary>
public sealed class HotSearchBoard
{
    /// <summary>榜单名称（如“短剧热搜榜”、“漫剧热搜榜”、“热点话题榜”）。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>榜单内的条目。</summary>
    public IReadOnlyList<HotSearchItem> Items { get; init; } = [];
}
