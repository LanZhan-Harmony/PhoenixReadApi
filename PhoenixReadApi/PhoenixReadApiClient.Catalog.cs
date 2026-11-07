namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取指定分类的一组剧集。</summary>
    /// <param name="category">
    /// 要获取的分类。使用 <see cref="PhoenixReadApiCategory.All"/> 获取全部分类，或使用真人剧、漫剧、
    /// AI 剧对应的枚举值获取单一分类。
    /// </param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按接口返回顺序排列的剧集列表；列表中的 ID 可传给 <see cref="GetDramaDetailsAsync"/>。</returns>
    /// <remarks>
    /// 分类接口可能需要多个分页请求，分页由客户端内部处理。结果是当前请求时刻可取得的列表，
    /// 不代表服务端全部剧集总数。
    /// </remarks>
    public Task<IReadOnlyList<Drama>> GetCatalogAsync(
        PhoenixReadApiCategory category = PhoenixReadApiCategory.All,
        CancellationToken cancellationToken = default
    ) => _catalog.GetAsync(category, cancellationToken);

    /// <summary>获取剧库的题材标签分类树（如热门、情节、主题、背景、角色等标签组）。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>题材标签分类分组列表。</returns>
    public Task<IReadOnlyList<TagCategoryGroup>> GetTagCategoriesAsync(
        CancellationToken cancellationToken = default
    ) => _catalog.GetTagCategoriesAsync(cancellationToken);

    /// <summary>按题材标签分页获取短剧列表。</summary>
    /// <param name="tagItemId">标签项标识（例如 "cate_261" 表示都市日常），可从 <see cref="GetTagCategoriesAsync"/> 获取，传空时获取全部。</param>
    /// <param name="sort">排序方式枚举，默认 <see cref="TagDramaSortOrder.Hot"/>（最热）。</param>
    /// <param name="offset">分页偏移量，初次获取传 0。</param>
    /// <param name="count">每页条数，范围 1 到 30，默认 12。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>当前页短剧列表及分页游标状态。</returns>
    public Task<TagDramaPage> GetTagDramaPageAsync(
        string tagItemId = "",
        TagDramaSortOrder sort = TagDramaSortOrder.Hot,
        int offset = 0,
        int count = 12,
        CancellationToken cancellationToken = default
    ) => _catalog.GetTagDramaPageAsync(tagItemId, sort, offset, count, cancellationToken);

    /// <summary>获取红果短剧演员排行榜分页数据。</summary>
    /// <param name="offset">分页偏移量，初次调用传入 0。</param>
    /// <param name="sessionId">上一页返回的会话 ID；初次请求可为 null。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含演员名次、人气、代表作及排名变动信息的 <see cref="ActorLeaderboardPage"/>。</returns>
    public Task<ActorLeaderboardPage> GetActorLeaderboardAsync(
        int offset = 0,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    ) => _catalog.GetActorLeaderboardAsync(offset, sessionId, cancellationToken);

    /// <summary>获取红果短剧排行榜分页数据。</summary>
    /// <param name="type">排行榜类型（推荐榜、热播榜、新剧榜、热搜榜），默认为 <see cref="ShortDramaLeaderboardType.Recommend"/>。</param>
    /// <param name="channel">频道受众（总榜、女频、男频），默认为 <see cref="ShortDramaLeaderboardChannel.All"/>。</param>
    /// <param name="genreTag">可选的题材标签 ID（例如 "cate_1051" 表示打脸虐渣，"cate_739" 表示逆袭）；非空时优先作为细分筛选条件。</param>
    /// <param name="offset">分页偏移量，初次调用传入 0。</param>
    /// <param name="sessionId">上一页返回的会话 ID；初次请求可为 null。</param>
    /// <param name="seenIds">已展示过的剧集 ID 集合，用于服务端跳过去重。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>短剧排行榜分页数据。</returns>
    public Task<ShortDramaLeaderboardPage> GetShortDramaLeaderboardAsync(
        ShortDramaLeaderboardType type = ShortDramaLeaderboardType.Recommend,
        ShortDramaLeaderboardChannel channel = ShortDramaLeaderboardChannel.All,
        string? genreTag = null,
        int offset = 0,
        string? sessionId = null,
        IEnumerable<string>? seenIds = null,
        CancellationToken cancellationToken = default
    ) =>
        _catalog.GetShortDramaLeaderboardAsync(
            type,
            channel,
            genreTag,
            offset,
            sessionId,
            seenIds,
            cancellationToken
        );

    /// <summary>流式遍历题材标签下的短剧，并在跨页时自动进行剧集 ID 去重。</summary>
    /// <param name="tagItemId">题材标签项标识（如 "cate_261"），可从 <see cref="GetTagCategoriesAsync"/> 获取，传空表示全部。</param>
    /// <param name="sort">排序方式，默认 <see cref="TagDramaSortOrder.Hot"/>（最热）。</param>
    /// <param name="pageSize">每页条数，范围 1 到 30，默认 12。</param>
    /// <param name="maxPages">最多拉取的页数上限，默认 10 页。</param>
    /// <param name="cancellationToken">用于取消流式遍历的令牌。</param>
    /// <returns>去重后的短剧异步流。</returns>
    public IAsyncEnumerable<Drama> EnumerateTagDramasAsync(
        string tagItemId = "",
        TagDramaSortOrder sort = TagDramaSortOrder.Hot,
        int pageSize = 12,
        int maxPages = 10,
        CancellationToken cancellationToken = default
    ) => _catalog.EnumerateTagDramasAsync(tagItemId, sort, pageSize, maxPages, cancellationToken);

    /// <summary>流式遍历短剧排行榜，并在跨页时自动去重。</summary>
    /// <param name="type">排行榜类型：默认为 <see cref="ShortDramaLeaderboardType.Recommend"/>（推荐榜）。</param>
    /// <param name="channel">频道受众：默认为 <see cref="ShortDramaLeaderboardChannel.All"/>（总榜）。</param>
    /// <param name="genreTag">可选细分标签。</param>
    /// <param name="maxPages">最多拉取的页数上限，默认 10 页。</param>
    /// <param name="cancellationToken">用于取消流式遍历的令牌。</param>
    /// <returns>去重后的排行榜短剧异步流。</returns>
    public IAsyncEnumerable<Drama> EnumerateShortDramaLeaderboardAsync(
        ShortDramaLeaderboardType type = ShortDramaLeaderboardType.Recommend,
        ShortDramaLeaderboardChannel channel = ShortDramaLeaderboardChannel.All,
        string? genreTag = null,
        int maxPages = 10,
        CancellationToken cancellationToken = default
    ) => _catalog.EnumerateShortDramaLeaderboardAsync(type, channel, genreTag, maxPages, cancellationToken);

    /// <summary>流式遍历演员排行榜。</summary>
    /// <param name="maxPages">最多拉取的页数上限，默认 10 页。</param>
    /// <param name="cancellationToken">用于取消流式遍历的令牌。</param>
    /// <returns>去重后的演员排行榜条目异步流。</returns>
    public IAsyncEnumerable<ActorLeaderboardItem> EnumerateActorLeaderboardAsync(
        int maxPages = 10,
        CancellationToken cancellationToken = default
    ) => _catalog.EnumerateActorLeaderboardAsync(maxPages, cancellationToken);

    /// <summary>获取 App 首页指定频道的发现页内容列表及流式翻页游标。</summary>
    /// <param name="channel">发现频道枚举，默认 <see cref="DiscoverChannel.Recommend"/>（推荐）。</param>
    /// <param name="cursor">分页游标（初次传 null，后续传上一页的 Cursor）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含短剧与小说列表及下一页游标的 <see cref="DiscoverPage"/>。</returns>
    public Task<DiscoverPage> GetDiscoverPageAsync(
        DiscoverChannel channel = DiscoverChannel.Recommend,
        string? cursor = null,
        CancellationToken cancellationToken = default
    ) => _discover.GetDiscoverPageAsync(channel, cursor, cancellationToken);

    /// <summary>获取短剧多维筛选面板选项列表（题材、角色、状态、排序等）。</summary>
    /// <param name="genre">短剧分类大类（真人剧、漫剧、AI剧），默认为 <see cref="ShortDramaGenre.ShortPlay"/>。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>筛选维度行及其可选选项列表。</returns>
    public Task<IReadOnlyList<ShortDramaFilterRow>> GetShortDramaFilterPanelAsync(
        ShortDramaGenre genre = ShortDramaGenre.ShortPlay,
        CancellationToken cancellationToken = default
    ) => _shortFilter.GetFilterPanelAsync(genre, cancellationToken);

    /// <summary>按多维筛选条件矩阵分页筛选短剧。</summary>
    /// <param name="query">多维筛选查询参数。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>筛选结果短剧页。</returns>
    public Task<ShortDramaFilterPage> FilterShortDramasAsync(
        ShortDramaFilterQuery query,
        CancellationToken cancellationToken = default
    ) => _shortFilter.FilterShortDramasAsync(query, cancellationToken);

    /// <summary>按指定日期查询短剧上新日历及预告排期剧集。</summary>
    /// <param name="targetDate">目标日期（如 2026-11-04），为 null 时查询今天。</param>
    /// <param name="offset">分页偏移量，初次请求传 0。</param>
    /// <param name="sessionId">上一页返回的会话 ID，初次传 null。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>当天上新及排期预告短剧结果页。</returns>
    public Task<DailyReleasePage> GetDailyReleasesAsync(
        DateOnly? targetDate = null,
        int offset = 0,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    ) => _dailyRelease.GetDailyReleasesAsync(targetDate, offset, sessionId, cancellationToken);

    /// <summary>获取原著小说/书籍分类落地页信息。</summary>
    /// <param name="categoryId">分类 ID。</param>
    /// <param name="offset">分页偏移量，默认 0。</param>
    /// <param name="count">返回条数，默认 20。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>原著小说落地页及书籍列表。</returns>
    public Task<NovelCategoryPage> GetNovelCategoryFrontAsync(
        string categoryId,
        int offset = 0,
        int count = 20,
        CancellationToken cancellationToken = default
    ) => _novelCategory.GetCategoryFrontAsync(categoryId, offset, count, cancellationToken);
}
