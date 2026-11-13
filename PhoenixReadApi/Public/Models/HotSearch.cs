namespace PhoenixReadApi;

/// <summary>搜索页热搜看板与猜你想搜数据。</summary>
public sealed class HotSearch
{
    /// <summary>“猜你想搜”短剧列表。</summary>
    public IReadOnlyList<HotSearchItem> Guesses { get; init; } = [];

    /// <summary>各类热搜榜单（如“短剧热搜榜”、“漫剧热搜榜”、“热点话题榜”）。</summary>
    public IReadOnlyList<HotSearchBoard> Boards { get; init; } = [];
}
