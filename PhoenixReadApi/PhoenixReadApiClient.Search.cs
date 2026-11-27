namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>综合搜索（返回包含短剧、合集、短视频、小说、用户、讨论与听书的混合联合结果）。</summary>
    /// <param name="keyword">搜索关键词，不能为空，最多 80 个字符。</param>
    /// <param name="offset">分页偏移量，从 0 开始。</param>
    /// <param name="count">每页返回数量（1～50）。</param>
    /// <param name="searchId">翻页时回传的服务端搜索会话 ID。</param>
    /// <param name="passback">翻页时回传的服务端透传参数。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按服务端混排顺序组织的 <see cref="ComprehensiveSearchItem"/> 分页结果。</returns>
    public Task<SearchPage<ComprehensiveSearchItem>> SearchComprehensiveAsync(
        string keyword,
        int offset = 0,
        int count = 20,
        string? searchId = null,
        string? passback = null,
        CancellationToken cancellationToken = default
    ) => _search.SearchComprehensiveAsync(keyword, offset, count, searchId, passback, cancellationToken);

    /// <summary>搜索短剧剧集（仅返回可直接调用 <see cref="GetDramaDetailsAsync"/> 的普通短剧）。</summary>
    /// <param name="keyword">搜索关键词，不能为空，最多 80 个字符。</param>
    /// <param name="offset">分页偏移量，从 0 开始。</param>
    /// <param name="count">每页返回数量（1～50）。</param>
    /// <param name="searchId">翻页时回传的服务端搜索会话 ID。</param>
    /// <param name="passback">翻页时回传的服务端透传参数。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>短剧分页结果。</returns>
    public Task<SearchPage<Drama>> SearchDramasAsync(
        string keyword,
        int offset = 0,
        int count = 20,
        string? searchId = null,
        string? passback = null,
        CancellationToken cancellationToken = default
    ) => _search.SearchDramasAsync(keyword, offset, count, searchId, passback, cancellationToken);

    /// <summary>搜索视频内容（返回合集 <see cref="VideoAlbum"/> 或独立短视频 <see cref="ShortVideo"/> 的联合类型）。</summary>
    /// <param name="keyword">搜索关键词，不能为空，最多 80 个字符。</param>
    /// <param name="offset">分页偏移量，从 0 开始。</param>
    /// <param name="count">每页返回数量（1～50）。</param>
    /// <param name="searchId">翻页时回传的服务端搜索会话 ID。</param>
    /// <param name="passback">翻页时回传的服务端透传参数。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>视频搜索结果分页。</returns>
    public Task<SearchPage<VideoSearchItem>> SearchVideosAsync(
        string keyword,
        int offset = 0,
        int count = 20,
        string? searchId = null,
        string? passback = null,
        CancellationToken cancellationToken = default
    ) => _search.SearchVideosAsync(keyword, offset, count, searchId, passback, cancellationToken);

    /// <summary>搜索图文讨论帖子（返回与红果圈图文共用的 <see cref="Post"/> 模型）。</summary>
    /// <param name="keyword">搜索关键词，不能为空，最多 80 个字符。</param>
    /// <param name="offset">分页偏移量，从 0 开始。</param>
    /// <param name="count">每页返回数量（1～50）。</param>
    /// <param name="searchId">翻页时回传的服务端搜索会话 ID。</param>
    /// <param name="passback">翻页时回传的服务端透传参数。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>讨论图文分页结果。</returns>
    public Task<SearchPage<Post>> SearchDiscussionsAsync(
        string keyword,
        int offset = 0,
        int count = 20,
        string? searchId = null,
        string? passback = null,
        CancellationToken cancellationToken = default
    ) => _search.SearchDiscussionsAsync(keyword, offset, count, searchId, passback, cancellationToken);

    /// <summary>搜索原著小说。</summary>
    /// <param name="keyword">搜索关键词，不能为空，最多 80 个字符。</param>
    /// <param name="offset">分页偏移量，从 0 开始。</param>
    /// <param name="count">每页返回数量（1～50）。</param>
    /// <param name="searchId">翻页时回传的服务端搜索会话 ID。</param>
    /// <param name="passback">翻页时回传的服务端透传参数。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>小说分页结果。</returns>
    public Task<SearchPage<Novel>> SearchNovelsAsync(
        string keyword,
        int offset = 0,
        int count = 20,
        string? searchId = null,
        string? passback = null,
        CancellationToken cancellationToken = default
    ) => _search.SearchNovelsAsync(keyword, offset, count, searchId, passback, cancellationToken);

    /// <summary>搜索演职人员与红果创作者用户。</summary>
    /// <param name="keyword">搜索关键词，不能为空，最多 80 个字符。</param>
    /// <param name="offset">分页偏移量，从 0 开始。</param>
    /// <param name="count">每页返回数量（1～50）。</param>
    /// <param name="searchId">翻页时回传的服务端搜索会话 ID。</param>
    /// <param name="passback">翻页时回传的服务端透传参数。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>用户分页结果。</returns>
    public Task<SearchPage<User>> SearchUsersAsync(
        string keyword,
        int offset = 0,
        int count = 20,
        string? searchId = null,
        string? passback = null,
        CancellationToken cancellationToken = default
    ) => _search.SearchUsersAsync(keyword, offset, count, searchId, passback, cancellationToken);

    /// <summary>搜索有声听书作品。</summary>
    /// <param name="keyword">搜索关键词，不能为空，最多 80 个字符。</param>
    /// <param name="offset">分页偏移量，从 0 开始。</param>
    /// <param name="count">每页返回数量（1～50）。</param>
    /// <param name="searchId">翻页时回传的服务端搜索会话 ID。</param>
    /// <param name="passback">翻页时回传的服务端透传参数。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>有声听书分页结果。</returns>
    public Task<SearchPage<Audiobook>> SearchAudiobooksAsync(
        string keyword,
        int offset = 0,
        int count = 20,
        string? searchId = null,
        string? passback = null,
        CancellationToken cancellationToken = default
    ) => _search.SearchAudiobooksAsync(keyword, offset, count, searchId, passback, cancellationToken);

    /// <summary>获取搜索联想建议。</summary>
    /// <param name="keyword">要联想的关键词，不能为空，最多 80 个字符。</param>
    /// <param name="maxResults">最多返回的建议数量，必须大于 0；超过 20 时按 20 处理。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>去重后的建议列表；每条建议包含文本和 <see cref="SearchSuggestionType"/>。</returns>
    public Task<IReadOnlyList<SearchSuggestion>> GetSuggestionsAsync(
        string keyword,
        int maxResults = 5,
        CancellationToken cancellationToken = default
    ) => _search.GetSuggestionsAsync(keyword, maxResults, cancellationToken);

    /// <summary>获取搜索框轮播提示词（搜索框默认占位与轮播推荐提示）。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>提示词列表，如“书名”、“主角”、“题材”等。</returns>
    public Task<IReadOnlyList<string>> GetSearchCuesAsync(
        CancellationToken cancellationToken = default
    ) => _search.GetSearchCuesAsync(cancellationToken);

    /// <summary>获取发现页搜索中间页的猜你想搜与热搜榜单。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含推荐词条及各类热搜榜的 <see cref="HotSearch"/>。</returns>
    public Task<HotSearch> GetHotSearchAsync(CancellationToken cancellationToken = default) =>
        _search.GetHotSearchAsync(cancellationToken);
}
