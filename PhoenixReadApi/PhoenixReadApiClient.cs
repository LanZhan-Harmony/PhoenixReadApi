using PhoenixReadApi.Internal.Features.Catalog;
using PhoenixReadApi.Internal.Features.Danmaku;
using PhoenixReadApi.Internal.Features.Details;
using PhoenixReadApi.Internal.Features.Actors;
using PhoenixReadApi.Internal.Features.Comments;
using PhoenixReadApi.Internal.Features.Playback;
using PhoenixReadApi.Internal.Features.Search;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi;

/// <summary>
/// PhoenixReadApi 的公共客户端入口。
/// </summary>
/// <remarks>客户端负责管理请求生命周期，并提供剧库、详情、搜索、播放和弹幕功能。</remarks>
public sealed class PhoenixReadApiClient : IDisposable
{
    private readonly PhoenixReadClientRuntime _runtime;
    private readonly CatalogService _catalog;
    private readonly DetailsService _details;
    private readonly SearchService _search;
    private readonly PlaybackService _playback;
    private readonly DanmakuService _danmaku;
    private readonly CommentService _comments;
    private readonly ActorService _actors;

    /// <summary>创建 API 客户端。</summary>
    public PhoenixReadApiClient(PhoenixReadApiOptions? options = null)
        : this(new HttpClient(), options, ownsHttpClient: true) { }

    /// <summary>使用由调用方管理的 <see cref="HttpClient"/> 创建 API 客户端。</summary>
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
        _actors = new ActorService(_runtime);
    }

    /// <summary>获取指定分类的剧集列表。</summary>
    public Task<IReadOnlyList<Drama>> GetCatalogAsync(
        PhoenixReadApiCategory category = PhoenixReadApiCategory.All,
        CancellationToken cancellationToken = default
    ) => _catalog.GetAsync(category, cancellationToken);

    /// <summary>获取剧集详情及分集列表。</summary>
    public Task<DramaDetails> GetDetailsAsync(
        string seriesId,
        CancellationToken cancellationToken = default
    ) => _details.GetAsync(seriesId, cancellationToken);

    /// <summary>获取指定分集的可播放媒体信息。</summary>
    public Task<PlaybackMedia> ResolveMediaAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _playback.ResolveAsync(seriesId, videoId, cancellationToken);

    /// <summary>获取指定分集的可播放媒体信息。</summary>
    public Task<PlaybackMedia> ResolveMediaAsync(
        Chapter chapter,
        string seriesId,
        CancellationToken cancellationToken = default
    ) => _playback.ResolveAsync(chapter, seriesId, cancellationToken);

    /// <summary>搜索剧集。</summary>
    public Task<SearchResult> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default
    ) => _search.SearchAsync(keyword, cancellationToken);

    /// <summary>获取规范化的搜索建议，默认返回五条，最多返回二十条。</summary>
    public Task<IReadOnlyList<SearchSuggestion>> GetSuggestionsAsync(
        string keyword,
        int maxResults = 5,
        CancellationToken cancellationToken = default
    ) => _search.GetSuggestionsAsync(keyword, maxResults, cancellationToken);

    /// <summary>获取一页推荐结果。</summary>
    public Task<RecommendationPage> GetRecommendationsAsync(
        RecommendationQuery query,
        CancellationToken cancellationToken = default
    ) => _search.GetRecommendationsAsync(query, cancellationToken);

    /// <summary>获取一页推荐结果。</summary>
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

    /// <summary>获取指定分集在指定时间范围内的弹幕。</summary>
    public Task<DanmakuPage> GetDanmakuAsync(
        string seriesId,
        string videoId,
        long startMs,
        long durationMs,
        CancellationToken cancellationToken = default
    ) => _danmaku.GetAsync(seriesId, videoId, startMs, durationMs, cancellationToken);

    /// <summary>获取剧评。</summary>
    public Task<CommentPage> GetReviewsAsync(
        string seriesId,
        int count = 10,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _comments.GetReviewsAsync(seriesId, count, cursor, cancellationToken);

    /// <summary>获取指定分集的评论。</summary>
    public Task<CommentPage> GetEpisodeCommentsAsync(
        string seriesId,
        string videoId,
        int count = 10,
        string cursor = "",
        CancellationToken cancellationToken = default
    ) => _comments.GetEpisodeCommentsAsync(seriesId, videoId, count, cursor, cancellationToken);

    /// <summary>获取演员主页及其参演剧集。传入剧集演员的 <see cref="DramaActor.ProfileId"/>。</summary>
    public Task<Actor> GetActorAsync(
        string profileId,
        CancellationToken cancellationToken = default
    ) => _actors.GetAsync(profileId, cancellationToken);

    /// <summary>获取剧集演员的主页，并保留详情中的演员资料。</summary>
    public Task<Actor> GetActorAsync(
        DramaActor actor,
        CancellationToken cancellationToken = default
    ) => _actors.GetAsync(actor, cancellationToken);

    /// <summary>销毁 API 客户端。</summary>
    public void Dispose() => _runtime.Dispose();
}
