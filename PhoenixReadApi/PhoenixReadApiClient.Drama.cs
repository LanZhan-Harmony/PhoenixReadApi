namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取一部剧的详情及其分集列表。</summary>
    /// <param name="dramaId">剧集 ID。可以传数字 ID，也可以传带有兼容前缀的 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>
    /// 包含 <see cref="DramaDetails.Drama"/> 和按集数排序的 <see cref="DramaDetails.Episodes"/>；
    /// 每个 <see cref="Episode"/> 都包含后续播放、评论和弹幕请求所需的 VideoId。
    /// </returns>
    /// <remarks>
    /// 通常应先调用此方法，再把返回的 <see cref="Episode"/> 传给播放、评论或弹幕方法，避免手动拼接视频 ID。
    /// </remarks>
    public Task<DramaDetails> GetDramaDetailsAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    ) => _drama.GetAsync(dramaId, cancellationToken);

    /// <summary>批量获取多部剧的基础元数据信息。</summary>
    /// <param name="dramaIds">要获取的剧集 ID 列表（单次最多 50 部）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>返回成功匹配的剧集列表。</returns>
    public Task<IReadOnlyList<Drama>> GetBatchDramaDetailsAsync(
        IEnumerable<string> dramaIds,
        CancellationToken cancellationToken = default
    ) => _drama.GetBatchDramaDetailsAsync(dramaIds, cancellationToken);

    /// <summary>批量获取多部短剧的完整详情及分集列表（调用播放器级批量详情接口）。</summary>
    /// <param name="dramaIds">要获取的短剧 ID 列表（单次最多 50 部）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按请求顺序返回的包含完整分集列表的 <see cref="DramaDetails"/> 集合。</returns>
    public Task<IReadOnlyList<DramaDetails>> GetBatchDramaDetailsWithEpisodesAsync(
        IEnumerable<string> dramaIds,
        CancellationToken cancellationToken = default
    ) => _drama.GetBatchDramaDetailsWithEpisodesAsync(dramaIds, cancellationToken);

    /// <summary>根据合集 ID 获取短剧多季合集详情及各季列表。</summary>
    /// <param name="albumId">多季合集 ID（可从 <see cref="DramaDetails.AlbumId"/> 获取）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含各季剧集元数据的 <see cref="DramaAlbum"/> 对象。</returns>
    public Task<DramaAlbum> GetAlbumAsync(
        string albumId,
        CancellationToken cancellationToken = default
    ) => _drama.GetAlbumAsync(albumId, cancellationToken);

    /// <summary>智能聚合短剧的所有季数（多源回退与派生聚合）。</summary>
    /// <param name="dramaId">当前短剧的 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按季数升序排列的完整季集合。</returns>
    public Task<DramaAlbum> GetDramaSeasonsAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    ) => _drama.GetDramaSeasonsAsync(dramaId, cancellationToken);

    /// <summary>获取指定短剧详情页的“相关作品”列表（同系列其他季、同 IP 短剧及原著小说）。</summary>
    /// <param name="dramaId">短剧 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>相关作品条目列表。</returns>
    public Task<IReadOnlyList<RelatedWorkItem>> GetDramaRelatedWorksAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    ) => _drama.GetDramaRelatedWorksAsync(dramaId, cancellationToken);

    /// <summary>获取指定短剧详情页的“猜你喜欢”推荐信息流（支持首页拉取与后续翻页）。</summary>
    /// <param name="dramaId">当前短剧 ID。</param>
    /// <param name="offset">分页偏移量；首次请求传 0，后续传入上一页的 <see cref="RecommendationFeedPage.NextOffset"/>。</param>
    /// <param name="cellId">“猜你喜欢”板块 ID；首次请求可传空字符串，翻页时传入上一页的 <see cref="RecommendationFeedPage.CellId"/>。</param>
    /// <param name="sessionId">推荐会话标识；翻页时传入上一页的 <see cref="RecommendationFeedPage.SessionId"/>。</param>
    /// <param name="limit">翻页时每页数量，范围为 1 到 50，默认为 10。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>“猜你喜欢”推荐分页结果（包含短剧与图文帖子）。</returns>
    public Task<RecommendationFeedPage> GetDramaGuessYouLikeAsync(
        string dramaId,
        int offset = 0,
        string cellId = "",
        string sessionId = "",
        int limit = 10,
        CancellationToken cancellationToken = default
    ) => _drama.GetDramaGuessYouLikeAsync(dramaId, offset, cellId, sessionId, limit, cancellationToken);

    /// <summary>获取单部短剧或视频合集播放完毕后的系列/相关推荐短剧。</summary>
    /// <param name="id">短剧 ID（当 <paramref name="isAlbum"/> 为 false）或合集 ID（当 <paramref name="isAlbum"/> 为 true）。</param>
    /// <param name="isAlbum">true 表示合集播完推荐，false 表示单部短剧播完底部系列剧推荐。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>推荐的下一部短剧；若无推荐则返回 null。</returns>
    public Task<Drama?> GetPlaybackEndRecommendationAsync(
        string id,
        bool isAlbum = false,
        CancellationToken cancellationToken = default
    ) => _drama.GetPlaybackEndRecommendationAsync(id, isAlbum, cancellationToken);
}
