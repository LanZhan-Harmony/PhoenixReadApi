using PhoenixReadApi.Internal.Features.Catalog;
using PhoenixReadApi.Internal.Features.Danmaku;
using PhoenixReadApi.Internal.Features.Details;
using PhoenixReadApi.Internal.Features.Playback;
using PhoenixReadApi.Internal.Features.Search;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi;

/// <summary>
/// Public facade for the Phoenix (红果) App and web APIs.
/// </summary>
/// <remarks>
/// The facade only owns the runtime and composes independent feature services. Transport,
/// caching, pagination, fallback and response mapping live in the corresponding service.
/// </remarks>
public class PhoenixReadApiClient : IDisposable
{
    /// <summary>Stable provider source name.</summary>
    public const string SourceName = "hongguo";

    /// <summary>Base application user agent prefix.</summary>
    public const string AppUserAgent = "com.phoenix.read/73532";

    private readonly PhoenixReadClientRuntime _runtime;
    private readonly CatalogService _catalog;
    private readonly DetailsService _details;
    private readonly SearchService _search;
    private readonly PlaybackService _playback;
    private readonly DanmakuService _danmaku;

    /// <summary>Creates a client using a new <see cref="HttpClient"/>.</summary>
    public PhoenixReadApiClient(PhoenixReadApiOptions? options = null)
        : this(new HttpClient(), options, ownsHttpClient: true) { }

    /// <summary>Creates a client using an application-owned <see cref="HttpClient"/>.</summary>
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
    }

    /// <summary>Gets the catalog, using App endpoints and then web pages when necessary.</summary>
    public Task<IReadOnlyList<Drama>> GetCatalogAsync(
        PhoenixReadApiCategory category = PhoenixReadApiCategory.All,
        CancellationToken cancellationToken = default
    ) => _catalog.GetAsync(category, cancellationToken);

    /// <summary>Gets a catalog response including fallback and partial-result diagnostics.</summary>
    public Task<CatalogResult> GetCatalogResultAsync(
        PhoenixReadApiCategory category = PhoenixReadApiCategory.All,
        CancellationToken cancellationToken = default
    ) => _catalog.GetResultAsync(category, cancellationToken);

    /// <summary>Gets details and an ordered, validated episode list for a series.</summary>
    public Task<DramaDetails> GetDetailsAsync(
        string seriesId,
        CancellationToken cancellationToken = default
    ) => _details.GetAsync(seriesId, cancellationToken);

    /// <summary>Resolves a chapter through App media, the web player and a backup endpoint.</summary>
    public Task<PlaybackMedia> ResolveMediaAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _playback.ResolveAsync(seriesId, videoId, cancellationToken);

    /// <summary>Resolves a chapter encoded as <c>hongguo-cenc://videoId</c>.</summary>
    public Task<PlaybackMedia> ResolveMediaAsync(
        Chapter chapter,
        string seriesId,
        CancellationToken cancellationToken = default
    ) => _playback.ResolveAsync(chapter, seriesId, cancellationToken);

    /// <summary>Searches the web index and App suggestions, merging duplicate series safely.</summary>
    public Task<SearchResult> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default
    ) => _search.SearchAsync(keyword, cancellationToken);

    /// <summary>Gets up to ten normalized search suggestions.</summary>
    public Task<IReadOnlyList<SearchSuggestion>> GetSuggestionsAsync(
        string keyword,
        CancellationToken cancellationToken = default
    ) => _search.GetSuggestionsAsync(keyword, cancellationToken);

    /// <summary>Requests one validated recommendation page.</summary>
    public Task<RecommendationPage> GetRecommendationsAsync(
        RecommendationQuery query,
        CancellationToken cancellationToken = default
    ) => _search.GetRecommendationsAsync(query, cancellationToken);

    /// <summary>Convenience overload for recommendation cursors.</summary>
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

    /// <summary>Gets a bounded danmaku window for an App episode.</summary>
    public Task<DanmakuPage> GetDanmakuAsync(
        string seriesId,
        string videoId,
        long startMs,
        long durationMs,
        CancellationToken cancellationToken = default
    ) => _danmaku.GetAsync(seriesId, videoId, startMs, durationMs, cancellationToken);

    /// <summary>Releases the internally-created HTTP client and service runtime.</summary>
    public void Dispose() => _runtime.Dispose();
}
