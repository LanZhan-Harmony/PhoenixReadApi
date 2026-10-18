using PhoenixReadApi.Internal.Features.Authentication;
using PhoenixReadApi.Internal.Features.Catalog;
using PhoenixReadApi.Internal.Features.Comments;
using PhoenixReadApi.Internal.Features.Danmaku;
using PhoenixReadApi.Internal.Features.Details;
using PhoenixReadApi.Internal.Features.Library;
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
    private readonly LibraryService _library;
    private readonly AuthenticationService _authentication;

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
        _library = new LibraryService(_runtime);
        _authentication = new AuthenticationService(_runtime);
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

    /// <summary>获取一条评论下的回复。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="count">本页最多返回的回复数，范围为 1 到 90。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>回复、总数、下一页游标和 HasMore 状态。</returns>
    public Task<CommentReplyPage> GetCommentRepliesAsync(
        string commentId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _comments.GetRepliesAsync(commentId, count, cursor, cancellationToken);

    /// <summary>获取一条剧评下的回复；这是 GetCommentRepliesAsync 的语义别名。</summary>
    /// <param name="commentId">剧评评论 ID。</param>
    /// <param name="count">本页数量。</param>
    /// <param name="cursor">上一页游标。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>回复分页结果。</returns>
    public Task<CommentReplyPage> GetReviewRepliesAsync(
        string commentId,
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => GetCommentRepliesAsync(commentId, count, cursor, cancellationToken);

    /// <summary>发送一条分集评论。</summary>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="text">评论文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>发送评论需要登录；服务端还可能进行内容审核。</remarks>
    public Task<PhoenixOperationResult> SendEpisodeCommentAsync(
        string seriesId,
        string videoId,
        string text,
        CancellationToken cancellationToken = default
    ) => _comments.SendCommentAsync(seriesId, videoId, text, cancellationToken);

    /// <summary>发送一条五分剧评（兼容重载）。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="text">剧评文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>发送剧评需要登录；新代码建议使用带 rating 的重载。</remarks>
    public Task<PhoenixOperationResult> SendDramaReviewAsync(
        string seriesId,
        string text,
        CancellationToken cancellationToken = default
    ) => _comments.SendReviewAsync(seriesId, 5.0, text, cancellationToken);

    /// <summary>按半星评分发送一条剧评。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="rating">用户评分，范围为 0.5 到 5.0，必须是 0.5 的倍数。</param>
    /// <param name="text">剧评文本，长度为 1 到 4000 个字符。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>红果协议中的 score 会自动按 0.5 星映射为 0 到 9。</remarks>
    public Task<PhoenixOperationResult> SendDramaReviewAsync(
        string seriesId,
        double rating,
        string text,
        CancellationToken cancellationToken = default
    ) => _comments.SendReviewAsync(seriesId, rating, text, cancellationToken);

    /// <summary>发送一条弹幕。</summary>
    /// <param name="seriesId">所属剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="text">弹幕文本。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>发送弹幕需要登录。当前接口不接受时间位置，服务端会按请求上下文处理。</remarks>
    public Task<PhoenixOperationResult> SendDanmakuAsync(
        string seriesId,
        string videoId,
        string text,
        CancellationToken cancellationToken = default
    ) => _comments.SendDanmakuAsync(seriesId, videoId, text, cancellationToken);

    /// <summary>点赞或取消点赞一条评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="groupId">评论所属的剧集或视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>该写操作需要登录。</remarks>
    public Task<PhoenixOperationResult> SetCommentLikeAsync(
        string commentId,
        string groupId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _comments.LikeCommentAsync(commentId, groupId, active, cancellationToken);

    /// <summary>删除自己发布的评论。</summary>
    /// <param name="commentId">评论 ID。</param>
    /// <param name="groupId">评论所属的剧集或视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>该操作需要登录，并且只能删除当前账号有权限删除的评论。</remarks>
    public Task<PhoenixOperationResult> DeleteCommentAsync(
        string commentId,
        string groupId,
        CancellationToken cancellationToken = default
    ) => _comments.DeleteCommentAsync(commentId, groupId, cancellationToken);

    /// <summary>回复一条评论。</summary>
    /// <param name="commentId">目标评论 ID。</param>
    /// <param name="text">回复文本。</param>
    /// <param name="replyToUserId">要回复的用户 ID；没有指定时传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>回复需要登录；服务端会校验目标评论和内容。</remarks>
    public Task<PhoenixOperationResult> SendCommentReplyAsync(
        string commentId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    ) => _comments.SendReplyAsync(commentId, text, replyToUserId, cancellationToken);

    /// <summary>按所属视频回复一条评论。</summary>
    /// <param name="commentId">目标评论 ID。</param>
    /// <param name="groupId">目标评论所属的视频或剧集 ID。</param>
    /// <param name="text">回复文本。</param>
    /// <param name="replyToUserId">要回复的用户 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SendCommentReplyAsync(
        string commentId,
        string groupId,
        string text,
        string replyToUserId,
        CancellationToken cancellationToken = default
    ) => _comments.SendReplyAsync(commentId, groupId, text, replyToUserId, cancellationToken);

    /// <summary>回复评论；这是 SendCommentReplyAsync 的简短别名。</summary>
    /// <param name="commentId">目标评论 ID。</param>
    /// <param name="text">回复文本。</param>
    /// <param name="replyToUserId">回复目标用户 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SendReplyAsync(
        string commentId,
        string text,
        string replyToUserId = "",
        CancellationToken cancellationToken = default
    ) => SendCommentReplyAsync(commentId, text, replyToUserId, cancellationToken);

    /// <summary>按所属视频回复一条评论；这是 SendCommentReplyAsync 的别名。</summary>
    public Task<PhoenixOperationResult> SendReplyAsync(
        string commentId,
        string groupId,
        string text,
        string replyToUserId,
        CancellationToken cancellationToken = default
    ) => SendCommentReplyAsync(commentId, groupId, text, replyToUserId, cancellationToken);

    /// <summary>点赞或取消点赞剧评。</summary>
    /// <param name="commentId">剧评评论 ID。</param>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetReviewLikeAsync(
        string commentId,
        string seriesId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) =>
        _comments.LikeCommentAsync(
            commentId,
            seriesId,
            active,
            cancellationToken,
            objectType: 2,
            commentType: 4
        );

    /// <summary>点赞或取消点赞弹幕。</summary>
    /// <param name="commentId">弹幕 ID。</param>
    /// <param name="videoId">视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetDanmakuLikeAsync(
        string commentId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) =>
        _comments.LikeCommentAsync(
            commentId,
            videoId,
            active,
            cancellationToken,
            objectType: 2,
            commentType: 20
        );

    /// <summary>点赞或取消点赞一条回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="groupId">回复所属的视频或剧集 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetReplyLikeAsync(
        string replyId,
        string groupId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) =>
        _comments.LikeCommentAsync(
            replyId,
            groupId,
            active,
            cancellationToken,
            objectType: 8,
            commentType: 4
        );

    /// <summary>删除一条回复。</summary>
    /// <param name="replyId">回复 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteReplyAsync(
        string replyId,
        CancellationToken cancellationToken = default
    ) => _comments.DeleteReplyAsync(replyId, cancellationToken);

    /// <summary>删除一条剧评。</summary>
    /// <param name="commentId">剧评 ID。</param>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteDramaReviewAsync(
        string commentId,
        string seriesId,
        CancellationToken cancellationToken = default
    ) =>
        _comments.DeleteCommentAsync(
            commentId,
            seriesId,
            cancellationToken,
            dataType: 4,
            serviceId: 2
        );

    /// <summary>删除一条弹幕。</summary>
    /// <param name="commentId">弹幕 ID。</param>
    /// <param name="videoId">视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteDanmakuAsync(
        string commentId,
        string videoId,
        CancellationToken cancellationToken = default
    ) =>
        _comments.DeleteCommentAsync(
            commentId,
            videoId,
            cancellationToken,
            dataType: 4,
            serviceId: 20
        );

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

    /// <summary>获取当前客户端账号的阅读历史。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="querySoftDeleted">是否包含已软删除的历史记录。</param>
    /// <param name="lastMinReadTimestampMilliseconds">只读取该时间之后的记录，0 表示不限制。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>阅读历史和分页状态。未登录账号通常会返回空列表。</returns>
    /// <remarks>红果会把短剧历史和其他阅读历史放在同一接口中，客户端只保留可识别的视频记录。</remarks>
    public Task<ReadHistoryPage> GetHistoryAsync(
        int count = 20,
        int offset = 0,
        bool querySoftDeleted = false,
        long lastMinReadTimestampMilliseconds = 0,
        CancellationToken cancellationToken = default
    ) =>
        _library.GetHistoryAsync(
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            cancellationToken
        );

    /// <summary>获取当前客户端的所有短剧播放进度。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端保存的播放进度；未登录账号可能返回设备级进度或空列表。</returns>
    public Task<ReadProgressPage> GetReadProgressAsync(
        CancellationToken cancellationToken = default
    ) => _library.GetReadProgressAsync(cancellationToken);

    /// <summary>按剧集或视频 ID 查询播放进度。</summary>
    /// <param name="seriesOrVideoIds">剧集 ID 或视频 ID，至少一个，最多 200 个。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>与输入 ID 匹配的播放进度。</returns>
    /// <remarks>调用方可以把搜索或详情返回的 ID 原样传入，不需要添加前缀。</remarks>
    public Task<ReadProgressPage> GetReadProgressForAsync(
        IReadOnlyList<string> seriesOrVideoIds,
        CancellationToken cancellationToken = default
    ) => _library.GetReadProgressForAsync(seriesOrVideoIds, cancellationToken);

    /// <summary>上报当前分集的播放进度。</summary>
    /// <param name="update">视频、剧集和播放位置；UseSoftDelete 为 true 时会删除历史。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>这是写入账号历史的接口，可能需要登录；不要高频调用，通常在暂停、退出或播放进度变化时调用。</remarks>
    public Task<PhoenixOperationResult> ReportProgressAsync(
        ProgressUpdate update,
        CancellationToken cancellationToken = default
    ) => _library.ReportProgressAsync(update, cancellationToken);

    /// <summary>删除当前账号的一条短剧阅读历史。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteHistoryAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _library.DeleteHistoryAsync(seriesId, videoId, cancellationToken);

    /// <summary>获取当前客户端账号收藏的剧集。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏剧集和分页状态；未登录账号通常返回空列表。</returns>
    /// <remarks>当前 App 收藏列表只返回剧集 ID，Title、Description 和 CoverUrl 可能为空；需要完整信息时再调用 GetDramaDetailsAsync。</remarks>
    public Task<DramaPage> GetFavoritesAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _library.GetFavoritesAsync(count, offset, cancellationToken);

    /// <summary>获取当前账号收藏剧集的 ID。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏剧集 ID 和分页状态。</returns>
    public async Task<(IReadOnlyList<string> Ids, DramaPage Page)> GetFavoriteIdsAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    )
    {
        DramaPage page = await GetFavoritesAsync(count, offset, cancellationToken)
            .ConfigureAwait(false);
        return (page.Items.Select(static item => item.Id).ToArray(), page);
    }

    /// <summary>取消收藏一部剧。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="videoId">用于定位收藏记录的分集视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> RemoveFavoriteAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _library.SetFavoriteAsync(seriesId, videoId, active: false, cancellationToken);

    /// <summary>获取当前客户端账号点赞的剧集。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>点赞剧集和分页状态；未登录账号通常返回空列表。</returns>
    public Task<DramaPage> GetLikesAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _library.GetLikesAsync(count, offset, cancellationToken);

    /// <summary>获取当前账号关注动态。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="cursor">上一页返回的游标；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>关注动态和游标分页状态。</returns>
    /// <remarks>该接口属于账号动态区域，通常需要登录；未登录时服务端会返回登录错误。</remarks>
    public Task<FollowUpdatePage> GetFollowUpdatesAsync(
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _library.GetFollowUpdatesAsync(count, cursor, cancellationToken);

    /// <summary>获取当前账号全部关注动态；这是 GetFollowUpdatesAsync 的语义别名。</summary>
    /// <param name="count">本页数量。</param>
    /// <param name="cursor">上一页游标。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>关注动态分页结果。</returns>
    public Task<FollowUpdatePage> GetAllFollowUpdatesAsync(
        int count = 20,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => GetFollowUpdatesAsync(count, cursor, cancellationToken);

    /// <summary>获取预约中的剧集。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>预约记录和分页状态。</returns>
    public Task<ReservationPage> GetReservationsAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _library.GetReservationsAsync(count, offset, calendar: false, cancellationToken);

    /// <summary>获取预约日历数据。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>预约记录；服务端提供的日历字段已转换为统一的预约实体。</returns>
    public Task<ReservationPage> GetCalendarAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _library.GetReservationsAsync(count, offset, calendar: true, cancellationToken);

    /// <summary>获取可预约的新剧列表。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">分页偏移量。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>可预约剧集及分页状态。</returns>
    public Task<ReservationPage> GetUpcomingReservationsAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _library.GetUpcomingReservationsAsync(count, offset, cancellationToken);

    /// <summary>设置或取消剧集预约。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="active">true 表示预约，false 表示取消预约。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetReservationAsync(
        string seriesId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _library.SetReservationAsync(seriesId, active, cancellationToken);

    /// <summary>取消剧集预约。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteReservationAsync(
        string seriesId,
        CancellationToken cancellationToken = default
    ) => _library.SetReservationAsync(seriesId, active: false, cancellationToken);

    /// <summary>根据筛选条件获取一页分类剧集。</summary>
    /// <param name="query">分类、筛选项和分页会话；不能为 null。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>剧集、可用筛选项和分页状态。</returns>
    public Task<CategoryPage> GetCategoryAsync(
        CategoryQuery query,
        CancellationToken cancellationToken = default
    ) => _library.GetCategoryAsync(query, cancellationToken);

    /// <summary>获取分类筛选面板和首屏内容；这是 GetCategoryAsync 的面向筛选场景别名。</summary>
    /// <param name="query">分类和筛选条件。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>筛选项、剧集和分页状态。</returns>
    public Task<CategoryPage> GetCategorySelectorAsync(
        CategoryQuery query,
        CancellationToken cancellationToken = default
    ) => GetCategoryAsync(query, cancellationToken);

    /// <summary>获取“新剧”等剧库单元的一页。</summary>
    /// <param name="offset">分页偏移量；首次请求使用 0。</param>
    /// <param name="sessionId">上一页返回的会话 ID；首次请求传空字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>分组剧集和分页状态。</returns>
    public Task<FeedPage> GetNewSeriesAsync(
        int offset = 0,
        string sessionId = "",
        CancellationToken cancellationToken = default
    ) => _library.GetNewSeriesAsync(offset, sessionId, cancellationToken);

    /// <summary>获取首页剧场和推荐分组。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按服务端分组返回的剧集。</returns>
    public Task<FeedPage> GetFeedCategoriesAsync(CancellationToken cancellationToken = default) =>
        _library.GetFeedCategoriesAsync(cancellationToken);

    /// <summary>获取剧库书架字段。</summary>
    /// <param name="channel">书架字段频道，例如短剧页面使用的频道标识。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端返回的字段键值。</returns>
    public Task<BookPackFields> GetBookPackFieldsAsync(
        string channel = "short_video",
        CancellationToken cancellationToken = default
    ) => _library.GetBookPackFieldsAsync(channel, cancellationToken);

    /// <summary>获取剧集的分季或关联剧集。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="totalEpisodeCount">当前剧集总集数；未知时传 0。</param>
    /// <param name="currentEpisode">当前集数；未知时传 0。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端返回的关联剧集；没有关联内容时为空列表。</returns>
    public Task<IReadOnlyList<Drama>> GetRelatedDramasAsync(
        string seriesId,
        int totalEpisodeCount = 0,
        int currentEpisode = 0,
        CancellationToken cancellationToken = default
    ) =>
        _library.GetRelatedDramasAsync(
            seriesId,
            totalEpisodeCount,
            currentEpisode,
            cancellationToken
        );

    /// <summary>获取一部剧的其他分季。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="totalEpisodeCount">当前剧集总集数；未知时传 0。</param>
    /// <param name="currentEpisode">当前集数；未知时传 0。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>其他分季剧集；没有分季时为空列表。</returns>
    public Task<IReadOnlyList<Drama>> GetSeriesSeasonsAsync(
        string seriesId,
        int totalEpisodeCount = 0,
        int currentEpisode = 0,
        CancellationToken cancellationToken = default
    ) =>
        _library.GetRelatedDramasAsync(
            seriesId,
            totalEpisodeCount,
            currentEpisode,
            cancellationToken
        );

    /// <summary>创建剧集分集分享链接。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端返回的分享 URL 和关联 ID。</returns>
    /// <remarks>部分节点暂未开放短链接生成；这种情况下仍返回 SeriesId 和 VideoId，Url 与 ShortUrl 为空。</remarks>
    public Task<ShareLink> CreateShareLinkAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _library.CreateShareLinkAsync(seriesId, videoId, cancellationToken);

    /// <summary>从分享 URL 中解析剧集和分集 ID。</summary>
    /// <param name="url">完整分享 URL。</param>
    /// <returns>解析出的分享目标；链接不含某个 ID 时对应属性为空字符串。</returns>
    public ShareLink ResolveShareLink(string url) => LibraryService.ResolveShareLink(url);

    /// <summary>收藏或取消收藏一部剧。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="videoId">用于定位收藏记录的分集视频 ID。</param>
    /// <param name="active">true 表示收藏，false 表示取消收藏。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetFavoriteAsync(
        string seriesId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _library.SetFavoriteAsync(seriesId, videoId, active, cancellationToken);

    /// <summary>关注或取消关注一个账号。</summary>
    /// <param name="userId">账号 ID。</param>
    /// <param name="active">true 表示关注，false 表示取消关注。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>该写操作需要登录。</remarks>
    public Task<PhoenixOperationResult> SetFollowAsync(
        string userId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _library.SetFollowAsync(userId, active, cancellationToken);

    /// <summary>点赞或取消点赞一部剧的一集。</summary>
    /// <param name="seriesId">剧集 ID。</param>
    /// <param name="videoId">用于定位剧集的分集视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    /// <remarks>该写操作需要登录。</remarks>
    public Task<PhoenixOperationResult> SetDramaLikeAsync(
        string seriesId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _library.SetDramaLikeAsync(seriesId, videoId, active, cancellationToken);

    /// <summary>向手机号发送短信验证码。</summary>
    /// <param name="phone">中国大陆手机号，可带或不带 <c>+86</c> 和空格。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>短信票据、脱敏手机号和再次发送等待时间。</returns>
    /// <remarks>调用会真实发送短信。登录接口可能进一步要求官方客户端完成人机验证。</remarks>
    public Task<VerificationCodeResult> SendVerificationCodeAsync(
        string phone,
        CancellationToken cancellationToken = default
    ) => _authentication.SendVerificationCodeAsync(phone, cancellationToken);

    /// <summary>使用短信验证码登录。</summary>
    /// <param name="phone">发送验证码时使用的中国大陆手机号。</param>
    /// <param name="code">收到的短信验证码。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>登录后的账号资料；会话 Cookie 保存在当前客户端实例中。</returns>
    /// <remarks>如果服务端要求额外的人机验证，方法会抛出提供方异常，需要先在官方客户端完成验证。</remarks>
    public Task<AuthenticatedUser> LoginAsync(
        string phone,
        string code,
        CancellationToken cancellationToken = default
    ) => _authentication.LoginAsync(phone, code, cancellationToken);

    /// <summary>读取当前客户端的登录状态。</summary>
    /// <returns>是否保存了登录会话以及最近一次登录返回的账号资料。</returns>
    public AuthenticationState GetAuthenticationState() => _authentication.GetState();

    /// <summary>清除当前客户端保存的登录 Cookie、Token、账号状态和本地会话备份。</summary>
    /// <remarks>不会撤销服务端会话，也不会影响调用方传入的 HttpClient。</remarks>
    public void Logout() => _authentication.Logout();

    /// <summary>释放 API 客户端使用的资源。</summary>
    /// <remarks>
    /// 如果客户端是通过默认构造函数创建的，调用此方法会释放内部 HttpClient；如果传入了外部 HttpClient，
    /// 只释放客户端自身的运行时状态，不会释放外部 HttpClient。重复调用是安全的。
    /// </remarks>
    public void Dispose() => _runtime.Dispose();
}
