using PhoenixReadApi.Internal.Features.Catalog;
using PhoenixReadApi.Internal.Features.Comments;
using PhoenixReadApi.Internal.Features.Danmaku;
using PhoenixReadApi.Internal.Features.Details;
using PhoenixReadApi.Internal.Features.Playback;
using PhoenixReadApi.Internal.Features.Search;
using PhoenixReadApi.Internal.Features.Users;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi;

/// <summary>
/// PhoenixReadApi 的公共客户端入口。
/// </summary>
/// <remarks>
/// 客户端负责管理请求生命周期，并提供剧库、详情、搜索、推荐、播放、弹幕、评论和账号主页功能。
/// 默认构造函数会创建并拥有一个 <see cref="HttpClient"/>；如果应用已经配置了代理、Cookie
/// 或自定义消息处理器，请使用注入 <see cref="HttpClient"/> 的构造函数。客户端使用完毕后应调用
/// <see cref="Dispose"/>，或者使用 using 声明。
/// </remarks>
public sealed class PhoenixReadApiClient : IDisposable
{
    private readonly PhoenixReadClientRuntime _runtime;
    private readonly CatalogService _catalog;
    private readonly DetailsService _details;
    private readonly SearchService _search;
    private readonly PlaybackService _playback;
    private readonly DanmakuService _danmaku;
    private readonly CommentService _comments;
    private readonly UserService _users;

    /// <summary>创建并拥有一个 API 客户端。</summary>
    /// <param name="options">
    /// 可选配置。可以设置请求超时、重试次数、代理所需的 <see cref="HttpClient"/> 注入方式，
    /// 以及是否启用网页兜底。传入 null 时使用默认配置。
    /// </param>
    /// <remarks>
    /// 此构造函数内部创建 <see cref="HttpClient"/>。如果需要复用连接、共享 Cookie 或使用代理，
    /// 应改用 <see cref="PhoenixReadApiClient(HttpClient, PhoenixReadApiOptions?)"/>。
    /// </remarks>
    public PhoenixReadApiClient(PhoenixReadApiOptions? options = null)
        : this(new HttpClient(), options, ownsHttpClient: true) { }

    /// <summary>使用由调用方管理的 <see cref="HttpClient"/> 创建 API 客户端。</summary>
    /// <param name="httpClient">
    /// 用于发送请求的 HTTP 客户端。可以在其处理器中配置代理、Cookie、连接池和诊断逻辑；不能为 null。
    /// </param>
    /// <param name="options">可选配置；为 null 时使用默认配置。</param>
    /// <remarks>
    /// 客户端不会销毁调用方传入的 <see cref="HttpClient"/>。调用 <see cref="Dispose"/> 后，
    /// 该 API 客户端不应继续使用，但传入的 HTTP 客户端仍由调用方负责管理。
    /// </remarks>
    public PhoenixReadApiClient(HttpClient httpClient, PhoenixReadApiOptions? options = null)
        : this(
            httpClient ?? throw new ArgumentNullException(nameof(httpClient)),
            options,
            ownsHttpClient: false
        ) { }

    private PhoenixReadApiClient(
        HttpClient httpClient,
        PhoenixReadApiOptions? options,
        bool ownsHttpClient
    )
    {
        _runtime = new PhoenixReadClientRuntime(httpClient, options, ownsHttpClient);
        _catalog = new CatalogService(_runtime);
        _details = new DetailsService(_runtime);
        _search = new SearchService(_runtime);
        _playback = new PlaybackService(_runtime);
        _danmaku = new DanmakuService(_runtime);
        _comments = new CommentService(_runtime);
        _users = new UserService(_runtime);
    }

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

    /// <summary>获取一部剧的详情及其分集列表。</summary>
    /// <param name="seriesId">剧集 ID。可以传数字 ID，也可以传带有兼容前缀的 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>
    /// 包含 <see cref="DramaDetails.Drama"/> 和按集数排序的 <see cref="DramaDetails.Episodes"/>；
    /// 每个 <see cref="Episode"/> 都包含后续播放、评论和弹幕请求所需的 VideoId。
    /// </returns>
    /// <remarks>
    /// 通常应先调用此方法，再把返回的 <see cref="Episode"/> 传给播放、评论或弹幕方法，避免手动拼接视频 ID。
    /// </remarks>
    public Task<DramaDetails> GetDramaDetailsAsync(
        string seriesId,
        CancellationToken cancellationToken = default
    ) => _details.GetAsync(seriesId, cancellationToken);

    /// <summary>根据剧集 ID 和视频 ID 获取指定分集的可播放媒体信息。</summary>
    /// <param name="seriesId">所属剧集 ID，用于校验分集归属和网页兜底请求。</param>
    /// <param name="videoId">分集视频 ID；可以传数字 ID 或兼容的 CENC 视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>
    /// 播放地址、Referer、媒体时长、最高可用清晰度、可用媒体版本和可能存在的 CENC 解密密钥。
    /// </returns>
    /// <remarks>
    /// 优先返回可用的高质量媒体版本，并将其他版本放在 <see cref="PlaybackMedia.Variants"/> 中。
    /// 如果返回了 <see cref="PlaybackMedia.DecryptionKey"/>，播放加密媒体时需要将其交给播放器。
    /// </remarks>
    public Task<PlaybackMedia> ResolveMediaAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _playback.ResolveAsync(seriesId, videoId, cancellationToken);

    /// <summary>根据详情中的 <see cref="Episode"/> 获取可播放媒体信息。</summary>
    /// <param name="seriesId">所属剧集 ID，用于校验分集归属和网页兜底请求。</param>
    /// <param name="episode">由 <see cref="GetDramaDetailsAsync"/> 返回的分集；不能为 null。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>与另一个播放重载相同的 <see cref="PlaybackMedia"/> 结果。</returns>
    /// <remarks>
    /// 推荐直接传入详情中的 Episode，这样可以避免从 Episode 再次读取或拼接 VideoId。
    /// </remarks>
    public Task<PlaybackMedia> ResolveMediaAsync(
        string seriesId,
        Episode episode,
        CancellationToken cancellationToken = default
    ) => _playback.ResolveAsync(seriesId, episode, cancellationToken);

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

    /// <summary>根据推荐查询获取一页推荐结果。</summary>
    /// <param name="query">
    /// 推荐查询。使用 <see cref="RecommendationQuery.Genre"/> 指定题材，Offset、SessionId 和 Seen
    /// 用于继续请求下一页；不能为 null。
    /// </param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>推荐剧集、下一页偏移量、会话 ID 和 HasMore 状态。</returns>
    /// <remarks>
    /// 当 <see cref="RecommendationPage.HasMore"/> 为 true 时，将返回的 NextOffset 和 SessionId
    /// 放入下一次 RecommendationQuery；Seen 可继续传入已展示的剧集 ID 以减少重复。
    /// </remarks>
    public Task<RecommendationPage> GetRecommendationsAsync(
        RecommendationQuery query,
        CancellationToken cancellationToken = default
    ) => _search.GetRecommendationsAsync(query, cancellationToken);

    /// <summary>使用题材和分页参数获取一页推荐结果。</summary>
    /// <param name="genre">推荐题材枚举，例如 <see cref="RecommendationGenre.ShortDrama"/>。</param>
    /// <param name="offset">分页偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="sessionId">上一页返回的会话 ID；首次请求可为 null。</param>
    /// <param name="seen">已经展示过的剧集 ID；可为 null，客户端会去重。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含当前页剧集和下一页状态的 <see cref="RecommendationPage"/>。</returns>
    /// <remarks>
    /// 该重载适合不需要手动构造 RecommendationQuery 的调用方；题材使用枚举，避免直接传输协议字符串。
    /// </remarks>
    public Task<RecommendationPage> GetRecommendationsAsync(
        RecommendationGenre genre,
        int offset = 0,
        string? sessionId = null,
        IEnumerable<string>? seen = null,
        CancellationToken cancellationToken = default
    ) => _search.GetRecommendationsAsync(genre, offset, sessionId, seen, cancellationToken);

    /// <summary>
    /// 获取指定分集在指定时间窗口内的弹幕。
    /// </summary>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="startMs">时间窗口起点（毫秒）。</param>
    /// <param name="durationMs">时间窗口长度（毫秒），不是结束时间。</param>
    /// <param name="cancellationToken">取消请求的令牌。</param>
    /// <returns>
    /// 自动合并、去重并按时间排序后的弹幕页。Total 是服务端报告的数量，Items.Count 是实际返回的记录数，
    /// 两者可能不同。
    /// </returns>
    /// <remarks>
    /// 客户端会根据服务端游标自动请求多个时间段，并对“有统计数量但返回空列表”的响应进行有限重试。
    /// durationMs 必须为正数；例如从视频开头读取十分钟可传 0 和 600000。
    /// </remarks>
    public Task<DanmakuPage> GetDanmakuAsync(
        string seriesId,
        string videoId,
        long startMs,
        long durationMs,
        CancellationToken cancellationToken = default
    ) => _danmaku.GetAsync(seriesId, videoId, startMs, durationMs, cancellationToken);

    /// <summary>获取一页剧评。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="count">本页最多返回的评论数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含评论、总数、下一页游标和 HasMore 状态的 <see cref="CommentPage"/>。</returns>
    /// <remarks>
    /// 当 HasMore 为 true 时，将返回页的 Cursor 原样传给下一次调用；不要自行解析或修改游标。
    /// </remarks>
    public Task<CommentPage> GetDramaReviewsAsync(
        string seriesId,
        int count = 10,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _comments.GetDramaReviewsAsync(seriesId, count, cursor, cancellationToken);

    /// <summary>获取指定分集的一页评论。</summary>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="count">本页最多返回的评论数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含评论、作者、总数、下一页游标和 HasMore 状态的 <see cref="CommentPage"/>。</returns>
    /// <remarks>当 HasMore 为 true 时，将 Cursor 原样传给下一次调用。</remarks>
    public Task<CommentPage> GetEpisodeCommentsAsync(
        string seriesId,
        string videoId,
        int count = 10,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _comments.GetEpisodeCommentsAsync(seriesId, videoId, count, cursor, cancellationToken);

    /// <summary>根据详情中的 <see cref="Episode"/> 获取一页分集评论。</summary>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="episode">由 <see cref="GetDramaDetailsAsync"/> 返回的分集；不能为 null。</param>
    /// <param name="count">本页最多返回的评论数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含评论、作者、总数、下一页游标和 HasMore 状态的 <see cref="CommentPage"/>。</returns>
    /// <remarks>当 HasMore 为 true 时，将 Cursor 原样传给下一次调用。</remarks>
    public Task<CommentPage> GetEpisodeCommentsAsync(
        string seriesId,
        Episode episode,
        int count = 10,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _comments.GetEpisodeCommentsAsync(seriesId, episode, count, cursor, cancellationToken);

    /// <summary>获取任意公开账号的主页及首批公开内容。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>
    /// 账号身份、首批收藏、点赞、动态和关联剧集，以及各内容集合的分页状态。
    /// </returns>
    /// <remarks>
    /// 该方法适合一次加载主页摘要。需要继续读取时，使用返回对象中的分页状态调用对应的
    /// GetUserActivitiesAsync、GetUserRelatedDramasAsync、GetUserCollectionsAsync 或 GetUserLikesAsync。
    /// </remarks>
    public Task<UserProfile> GetUserProfileAsync(
        string userId,
        CancellationToken cancellationToken = default
    ) => _users.GetAsync(userId, cancellationToken);

    /// <summary>根据剧集署名关系获取对应账号的主页。</summary>
    /// <param name="credit">详情中的剧集署名关系；方法会使用其中的 User ID 和已有资料。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>账号主页及首批公开内容和分页状态。</returns>
    /// <remarks>使用此重载可以保留详情中已经取得的账号名称、简介和头像。</remarks>
    public Task<UserProfile> GetUserProfileAsync(
        DramaCredit credit,
        CancellationToken cancellationToken = default
    ) => _users.GetAsync(credit, cancellationToken);

    /// <summary>获取账号关联剧集的一页。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>关联剧集及 Cursor、HasMore 和 Total 分页信息。</returns>
    /// <remarks>当 HasMore 为 true 时，将 Cursor 原样传给下一次调用。</remarks>
    public Task<UserDramaPage> GetUserRelatedDramasAsync(
        string userId,
        int count = 18,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _users.GetRelatedDramasAsync(userId, count, cursor, cancellationToken);

    /// <summary>获取账号动态的一页。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="cursor">上一页返回的 Cursor；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>动态记录及 Cursor、HasMore 和 Total 分页信息。</returns>
    /// <remarks>当 HasMore 为 true 时，将 Cursor 原样传给下一次调用。</remarks>
    public Task<UserActivityPage> GetUserActivitiesAsync(
        string userId,
        int count = 18,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _users.GetActivitiesAsync(userId, count, cursor, cancellationToken);

    /// <summary>获取账号收藏剧集的一页。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">分页偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏剧集及 NextOffset、HasMore 和 Total 分页信息。</returns>
    /// <remarks>当 HasMore 为 true 时，将 NextOffset 原样传给下一次调用。</remarks>
    public Task<UserDramaPage> GetUserCollectionsAsync(
        string userId,
        int count = 18,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _users.GetCollectionsAsync(userId, count, offset, cancellationToken);

    /// <summary>获取账号点赞剧集的一页。</summary>
    /// <param name="userId">账号 ID，必须是数字字符串。</param>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">分页偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>点赞剧集及 NextOffset、HasMore 和 Total 分页信息。</returns>
    /// <remarks>当 HasMore 为 true 时，将 NextOffset 原样传给下一次调用。</remarks>
    public Task<UserDramaPage> GetUserLikesAsync(
        string userId,
        int count = 18,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _users.GetLikesAsync(userId, count, offset, cancellationToken);

    /// <summary>释放 API 客户端使用的资源。</summary>
    /// <remarks>
    /// 如果客户端是通过默认构造函数创建的，调用此方法会释放内部 HttpClient；如果传入了外部 HttpClient，
    /// 只释放客户端自身的运行时状态，不会释放外部 HttpClient。重复调用是安全的。
    /// </remarks>
    public void Dispose() => _runtime.Dispose();
}
