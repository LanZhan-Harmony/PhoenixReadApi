namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>搜索剧集。</summary>
    /// <param name="keyword">搜索关键词，支持剧名、别名或相关名称；不能为空，最多 80 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>
    /// 搜索结果及总数、是否受限和非致命警告。结果中的 <see cref="Drama.Id"/> 可传给 <see cref="GetDramaDetailsAsync"/>。
    /// </returns>
    /// <remarks>
    /// 搜索会合并名称匹配和综合搜索结果；网络或服务端部分失败时可能返回有限结果并设置
    /// <see cref="SearchResult.Warning"/>，调用方应同时检查 <see cref="SearchResult.Limited"/>。
    /// </remarks>
    public Task<SearchResult> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default
    ) => _search.SearchAsync(keyword, cancellationToken);

    /// <summary>使用自定义分页游标与分类参数高级搜索剧集。</summary>
    /// <param name="query">搜索高级查询参数，包含关键词、偏移量、期望返回条数及标签类型。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含当前页剧集、下一页偏移量与更多状态的搜索结果。</returns>
    public Task<SearchResult> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default
    ) => _search.SearchAsync(query, cancellationToken);

    /// <summary>获取搜索联想建议。</summary>
    /// <param name="keyword">要联想的关键词，不能为空，最多 80 个字符。</param>
    /// <param name="maxResults">最多返回的建议数量，必须大于 0；超过 20 时按 20 处理。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>去重后的建议列表；每条建议包含文本和 <see cref="SearchSuggestionType"/>。</returns>
    /// <remarks>
    /// 建议结果会按关键词和数量短暂缓存。建议类型是枚举，未知的后端类型会映射为 Unknown。
    /// </remarks>
    public Task<IReadOnlyList<SearchSuggestion>> GetSuggestionsAsync(
        string keyword,
        int maxResults = 5,
        CancellationToken cancellationToken = default
    ) => _search.GetSuggestionsAsync(keyword, maxResults, cancellationToken);

    /// <summary>使用题材和分页参数获取一页推荐结果。</summary>
    /// <param name="genre">推荐题材枚举，例如 <see cref="RecommendationGenre.ShortDrama"/>。</param>
    /// <param name="offset">分页偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="sessionId">上一页返回的会话 ID；首次请求可为 null。</param>
    /// <param name="seen">已经展示过的剧集 ID；可为 null，客户端会去重。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含当前页剧集和下一页状态的 <see cref="RecommendationPage"/>。</returns>
    public Task<RecommendationPage> GetRecommendationsAsync(
        RecommendationGenre genre,
        int offset = 0,
        string? sessionId = null,
        IEnumerable<string>? seen = null,
        CancellationToken cancellationToken = default
    ) => _search.GetRecommendationsAsync(genre, offset, sessionId, seen, cancellationToken);

    /// <summary>获取搜索框轮播提示词（搜索框默认占位与轮播推荐提示）。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>提示词列表，如“书名”、“主角”、“题材”等。</returns>
    public Task<IReadOnlyList<string>> GetSearchCuesAsync(
        CancellationToken cancellationToken = default
    ) => _search.GetSearchCuesAsync(cancellationToken);

    /// <summary>获取搜索页的“猜你想搜”及各个热搜榜单（短剧热搜榜、漫剧热搜榜、热点话题榜等）。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含“猜你想搜”短剧及热搜各榜单的 <see cref="HotSearch"/> 对象。</returns>
    public Task<HotSearch> GetHotSearchAsync(CancellationToken cancellationToken = default) =>
        _search.GetHotSearchAsync(cancellationToken);

    /// <summary>流式遍历指定题材的推荐短剧，并在跨页时自动追踪并过滤重复的剧集。</summary>
    /// <param name="genre">推荐题材分类枚举。</param>
    /// <param name="maxPages">最多拉取的页数上限，默认 10 页。</param>
    /// <param name="cancellationToken">用于取消流式遍历的令牌。</param>
    /// <returns>去重后的短剧异步流。</returns>
    public IAsyncEnumerable<Drama> EnumerateRecommendationsAsync(
        RecommendationGenre genre = RecommendationGenre.ShortDrama,
        int maxPages = 10,
        CancellationToken cancellationToken = default
    ) => _search.EnumerateRecommendationsAsync(genre, maxPages, cancellationToken);
}
