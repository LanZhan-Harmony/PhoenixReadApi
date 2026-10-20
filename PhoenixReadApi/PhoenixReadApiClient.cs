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
public sealed partial class PhoenixReadApiClient : IDisposable
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
}
