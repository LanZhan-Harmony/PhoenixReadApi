namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
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
}
