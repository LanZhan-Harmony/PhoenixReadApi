namespace PhoenixReadApi;

/// <summary>短剧搜索高级查询参数。</summary>
public sealed class SearchQuery
{
    /// <summary>搜索关键词，支持剧名、演员或题材；不能为空，长度在 1 至 80 字符之间。</summary>
    public string Keyword { get; init; } = string.Empty;

    /// <summary>搜索结果的起始偏移量（第一页使用 0，后续使用上一页返回的 <see cref="SearchResult.NextOffset"/>）。</summary>
    public int Offset { get; init; }

    /// <summary>单页期望返回的条数，范围为 1 至 50，默认为 20。</summary>
    public int Count { get; init; } = 20;

    /// <summary>要查询的分类标签，默认为 <see cref="SearchTab.ShortPlay"/>（短剧）。</summary>
    public SearchTab Tab { get; init; } = SearchTab.ShortPlay;

    /// <summary>分页查询会话标识（首次请求传 null，后续翻页可传入上一页返回的 <see cref="SearchResult.SearchId"/>）。</summary>
    public string? SearchId { get; init; }

    /// <summary>分页回传令牌（可选；未提供且 Offset &gt; 0 时自动使用当前偏移量）。</summary>
    public string? Passback { get; init; }
}
