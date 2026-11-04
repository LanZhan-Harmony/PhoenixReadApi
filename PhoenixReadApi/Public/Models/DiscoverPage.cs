namespace PhoenixReadApi;

/// <summary>App 首页频道发现流结果页面。</summary>
/// <remarks>
/// 对应红果 App 底层频道流式 Feed（接口为 bookmall/tab 及 cell/change）。
/// 根据请求的频道不同，包含短剧或原著小说。
/// </remarks>
public sealed class DiscoverPage
{
    /// <summary>当前页短剧条目（推荐、真人剧、漫剧、找剧等短剧频道返回）。</summary>
    public IReadOnlyList<Drama> Dramas { get; init; } = [];

    /// <summary>当前页原著小说条目（小说、听书频道或小说卡片返回）。</summary>
    public IReadOnlyList<Novel> Novels { get; init; } = [];

    /// <summary>获取下一批数据所需的流式分页游标；若无更多则为空字符串。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>是否还有更多内容。</summary>
    public bool HasMore { get; init; }
}
