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
    private readonly HistoryService _history;
    private readonly FavoritesService _favorites;
    private readonly LikesService _likes;
    private readonly FollowsService _follows;
    private readonly ReservationsService _reservations;
    private readonly DiscoveryService _discovery;
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
        _history = new HistoryService(_runtime);
        _favorites = new FavoritesService(_runtime);
        _likes = new LikesService(_runtime);
        _follows = new FollowsService(_runtime);
        _reservations = new ReservationsService(_runtime);
        _discovery = new DiscoveryService(_runtime);
        _authentication = new AuthenticationService(_runtime);
    }

    /// <summary>释放 API 客户端使用的资源。</summary>
    /// <remarks>
    /// 如果客户端是通过默认构造函数创建的，调用此方法会释放内部 HttpClient；如果传入了外部 HttpClient，
    /// 只释放客户端自身的运行时状态，不会释放外部 HttpClient。重复调用是安全的。
    /// </remarks>
    public void Dispose() => _runtime.Dispose();
}
