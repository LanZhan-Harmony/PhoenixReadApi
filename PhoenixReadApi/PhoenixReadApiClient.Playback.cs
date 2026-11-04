namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>根据剧集 ID 和视频 ID 解析指定分集的可播放媒体列表（按清晰度综合得分从高到低排序）。</summary>
    /// <param name="seriesId">所属剧集 ID，用于校验分集归属和网页兜底请求。</param>
    /// <param name="videoId">分集视频 ID；可以传数字 ID 或兼容的 CENC 视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>
    /// 按画质综合得分从高到低排序的可播放媒体列表；每个媒体都具备完整的播放直链、清晰度枚举、编码、Referer 及可能存在的 CENC 解密密钥。
    /// 列表中首个条目即为推荐的最佳画质。
    /// </returns>
    /// <remarks>
    /// 如果返回的条目包含 <see cref="PlaybackMedia.DecryptionKey"/>，播放加密媒体时需要将其交给播放器内核。
    /// </remarks>
    public Task<IReadOnlyList<PlaybackMedia>> ResolveMediaAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _playback.ResolveAsync(seriesId, videoId, cancellationToken);
}
