namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取指定独立短视频或合集子视频的完整元数据、作者信息与关联短剧。</summary>
    /// <param name="videoId">短视频 ID，必须是数字字符串。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>短视频的详细资料实体。</returns>
    public Task<ShortVideo> GetShortVideoDetailsAsync(
        string videoId,
        CancellationToken cancellationToken = default
    ) => _circle.GetCircleVideoAsync(videoId, cancellationToken);
}
