namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
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

    /// <summary>批量获取多部剧的基础元数据信息。</summary>
    /// <param name="seriesIds">要获取的剧集 ID 列表（单次最多 50 部）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>返回成功匹配的剧集列表。</returns>
    public Task<IReadOnlyList<Drama>> GetBatchDramaDetailsAsync(
        IEnumerable<string> seriesIds,
        CancellationToken cancellationToken = default
    ) => _details.GetBatchDramaDetailsAsync(seriesIds, cancellationToken);
}
