using PhoenixReadApi.Internal.Features.Catalog;
using PhoenixReadApi.Internal.Features.Danmaku;
using PhoenixReadApi.Internal.Features.Details;
using PhoenixReadApi.Internal.Features.Playback;
using PhoenixReadApi.Internal.Features.Search;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi;

/// <summary>
/// 红果 App 和网页 API 的公共客户端入口。
/// </summary>
/// <remarks>
/// 该入口类只负责维护运行时并组合各功能服务；传输、
/// 缓存、分页、兜底和响应映射分别由对应服务负责。
/// </remarks>
public class PhoenixReadApi : IDisposable
{
    /// <summary>固定的数据源名称。</summary>
    public const string SourceName = "hongguo";

    /// <summary>应用 User-Agent 的基础前缀。</summary>
    public const string AppUserAgent = "com.phoenix.read/73532";

    private readonly PhoenixReadClientRuntime _runtime;
    private readonly CatalogService _catalog;
    private readonly DetailsService _details;
    private readonly SearchService _search;
    private readonly PlaybackService _playback;
    private readonly DanmakuService _danmaku;

    /// <summary>使用新的 <see cref="HttpClient"/> 创建客户端。</summary>
    public PhoenixReadApi(PhoenixReadApiOptions? options = null)
        : this(new HttpClient(), options, ownsHttpClient: true) { }

    /// <summary>使用由应用管理的 <see cref="HttpClient"/> 创建客户端。</summary>
    public PhoenixReadApi(HttpClient httpClient, PhoenixReadApiOptions? options = null)
        : this(
            httpClient ?? throw new ArgumentNullException(nameof(httpClient)),
            options,
            ownsHttpClient: false
        ) { }

    private PhoenixReadApi(
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
    }

    /// <summary>获取剧库；必要时先请求 App 接口，再使用网页接口兜底。</summary>
    public Task<IReadOnlyList<Drama>> GetCatalogAsync(
        PhoenixReadApiCategory category = PhoenixReadApiCategory.All,
        CancellationToken cancellationToken = default
    ) => _catalog.GetAsync(category, cancellationToken);

    /// <summary>获取剧库结果，并附带兜底过程和部分结果的诊断信息。</summary>
    public Task<CatalogResult> GetCatalogResultAsync(
        PhoenixReadApiCategory category = PhoenixReadApiCategory.All,
        CancellationToken cancellationToken = default
    ) => _catalog.GetResultAsync(category, cancellationToken);

    /// <summary>获取剧集详情及经过校验的有序分集列表。</summary>
    public Task<DramaDetails> GetDetailsAsync(
        string seriesId,
        CancellationToken cancellationToken = default
    ) => _details.GetAsync(seriesId, cancellationToken);

    /// <summary>依次通过 App 媒体、网页播放器和备用接口解析分集。</summary>
    public Task<PlaybackMedia> ResolveMediaAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _playback.ResolveAsync(seriesId, videoId, cancellationToken);

    /// <summary>解析编码为 <c>hongguo-cenc://videoId</c> 的分集地址。</summary>
    public Task<PlaybackMedia> ResolveMediaAsync(
        Chapter chapter,
        string seriesId,
        CancellationToken cancellationToken = default
    ) => _playback.ResolveAsync(chapter, seriesId, cancellationToken);

    /// <summary>搜索网页索引和 App 搜索建议，并安全合并重复剧集。</summary>
    public Task<SearchResult> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default
    ) => _search.SearchAsync(keyword, cancellationToken);

    /// <summary>获取最多十条规范化的搜索建议。</summary>
    public Task<IReadOnlyList<SearchSuggestion>> GetSuggestionsAsync(
        string keyword,
        CancellationToken cancellationToken = default
    ) => _search.GetSuggestionsAsync(keyword, cancellationToken);

    /// <summary>请求一页经过校验的推荐结果。</summary>
    public Task<RecommendationPage> GetRecommendationsAsync(
        RecommendationQuery query,
        CancellationToken cancellationToken = default
    ) => _search.GetRecommendationsAsync(query, cancellationToken);

    /// <summary>用于推荐分页游标的快捷重载。</summary>
    public Task<RecommendationPage> GetRecommendationsAsync(
        string genre,
        int offset = 0,
        string? sessionId = null,
        IEnumerable<string>? seen = null,
        CancellationToken cancellationToken = default
    ) =>
        _search.GetRecommendationsAsync(
            new RecommendationQuery
            {
                Genre = genre ?? string.Empty,
                Offset = offset,
                SessionId = sessionId ?? string.Empty,
                Seen = [.. (seen ?? [])],
            },
            cancellationToken
        );

    /// <summary>获取 App 分集在指定时间范围内的弹幕。</summary>
    public Task<DanmakuPage> GetDanmakuAsync(
        string seriesId,
        string videoId,
        long startMs,
        long durationMs,
        CancellationToken cancellationToken = default
    ) => _danmaku.GetAsync(seriesId, videoId, startMs, durationMs, cancellationToken);

    /// <summary>释放内部创建的 HTTP 客户端及服务运行时。</summary>
    public void Dispose() => _runtime.Dispose();
}
