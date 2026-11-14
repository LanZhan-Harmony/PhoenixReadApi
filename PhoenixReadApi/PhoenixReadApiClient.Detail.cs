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
    ) => _details.GetAsync(dramaId, cancellationToken);

    /// <summary>批量获取多部剧的基础元数据信息。</summary>
    /// <param name="dramaIds">要获取的剧集 ID 列表（单次最多 50 部）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>返回成功匹配的剧集列表。</returns>
    public Task<IReadOnlyList<Drama>> GetBatchDramaDetailsAsync(
        IEnumerable<string> dramaIds,
        CancellationToken cancellationToken = default
    ) => _details.GetBatchDramaDetailsAsync(dramaIds, cancellationToken);

    /// <summary>批量获取多部短剧的完整详情及分集列表（调用播放器级批量详情接口）。</summary>
    /// <param name="dramaIds">要获取的短剧 ID 列表（单次最多 50 部）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按请求顺序返回的包含完整分集列表的 <see cref="DramaDetails"/> 集合。</returns>
    public Task<IReadOnlyList<DramaDetails>> GetBatchDramaDetailsWithEpisodesAsync(
        IEnumerable<string> dramaIds,
        CancellationToken cancellationToken = default
    ) => _details.GetBatchDramaDetailsWithEpisodesAsync(dramaIds, cancellationToken);

    /// <summary>根据合集 ID 获取短剧多季合集详情及各季列表。</summary>
    /// <param name="albumId">多季合集 ID（可从 <see cref="DramaDetails.RelatedAlbumId"/> 获取）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含各季剧集元数据的 <see cref="DramaAlbum"/> 对象。</returns>
    public Task<DramaAlbum> GetAlbumAsync(
        string albumId,
        CancellationToken cancellationToken = default
    ) => _details.GetAlbumAsync(albumId, cancellationToken);

    /// <summary>智能聚合短剧的所有季数（多源回退与派生聚合）。</summary>
    /// <param name="dramaId">当前短剧的 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按季数升序排列的完整季集合。</returns>
    public Task<DramaAlbum> GetDramaSeasonsAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    ) => _details.GetDramaSeasonsAsync(dramaId, cancellationToken);

    /// <summary>获取指定短剧的专属关联推荐好剧（播放器/详情页相关推荐）。</summary>
    /// <param name="dramaId">短剧 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含推荐列表和推荐标题的 <see cref="DramaRecommendations"/> 对象。</returns>
    public Task<DramaRecommendations> GetDramaRecommendationsAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    ) => _details.GetDramaRecommendationsAsync(dramaId, cancellationToken);

    /// <summary>获取指定短剧的专属关联推荐好剧（向后兼容重命名别名）。</summary>
    public Task<DramaRecommendations> GetSeriesRecommendationsAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    ) => GetDramaRecommendationsAsync(dramaId, cancellationToken);
}
