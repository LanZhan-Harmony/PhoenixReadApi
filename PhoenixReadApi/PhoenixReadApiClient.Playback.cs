namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>根据剧集 ID 和分集视频 ID 解析普通短剧分集的可播放媒体列表（按画质综合得分从高到低排序）。</summary>
    /// <param name="dramaId">所属短剧 ID，用于校验分集归属与网页兜底请求。</param>
    /// <param name="videoId">分集视频 ID；可传数字 ID 或兼容的 CENC 视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按画质综合得分从高到低排序的可播放媒体列表，首个条目即为推荐最高画质。</returns>
    public Task<IReadOnlyList<PlaybackMedia>> ResolveDramaMediaAsync(
        string dramaId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _playback.ResolveDramaAsync(dramaId, videoId, cancellationToken);

    /// <summary>根据剧集 ID 和分集实体解析普通短剧分集的可播放媒体列表。</summary>
    /// <param name="dramaId">所属短剧 ID。</param>
    /// <param name="episode">分集详情实体，内部自动提取 <see cref="Episode.VideoId"/>。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按画质综合得分从高到低排序的可播放媒体列表。</returns>
    public Task<IReadOnlyList<PlaybackMedia>> ResolveDramaMediaAsync(
        string dramaId,
        Episode episode,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(episode);
        return _playback.ResolveDramaAsync(dramaId, episode.VideoId, cancellationToken);
    }

    /// <summary>解析独立短视频（红果圈短视频、视频搜索结果或合集子视频）的可播放媒体列表。</summary>
    /// <param name="videoId">短视频 ID 或合集子视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按画质综合得分从高到低排序的可播放媒体列表。</returns>
    public Task<IReadOnlyList<PlaybackMedia>> ResolveShortVideoMediaAsync(
        string videoId,
        CancellationToken cancellationToken = default
    ) => _playback.ResolveShortVideoAsync(videoId, cancellationToken);

    /// <summary>根据 <see cref="ShortVideo"/> 实体解析独立短视频的可播放媒体列表。</summary>
    /// <param name="video">短视频实体，自动优先读取 <see cref="ShortVideo.VideoId"/> 或 <see cref="ShortVideo.Id"/>。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按画质综合得分从高到低排序的可播放媒体列表。</returns>
    public Task<IReadOnlyList<PlaybackMedia>> ResolveShortVideoMediaAsync(
        ShortVideo video,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(video);
        string videoId = !string.IsNullOrWhiteSpace(video.VideoId) ? video.VideoId : video.Id;
        return _playback.ResolveShortVideoAsync(videoId, cancellationToken);
    }

    /// <summary>根据合集子视频条目 <see cref="DramaSeason"/> 解析对应短视频的可播放媒体列表。</summary>
    /// <param name="season">通过 <see cref="GetAlbumAsync"/> 获取到的合集子项。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按画质综合得分从高到低排序的可播放媒体列表。</returns>
    public Task<IReadOnlyList<PlaybackMedia>> ResolveShortVideoMediaAsync(
        DramaSeason season,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(season);
        return _playback.ResolveShortVideoAsync(season.DramaId, cancellationToken);
    }
}
