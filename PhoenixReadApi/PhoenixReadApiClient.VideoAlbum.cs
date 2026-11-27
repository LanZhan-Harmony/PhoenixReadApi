namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>根据合集 ID 获取视频合集详情及收录的短视频列表。</summary>
    /// <param name="albumId">视频合集 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含合集元数据与短视频列表的 <see cref="VideoAlbumDetails"/> 对象。</returns>
    public Task<VideoAlbumDetails> GetVideoAlbumDetailsAsync(
        string albumId,
        CancellationToken cancellationToken = default
    ) => _drama.GetVideoAlbumDetailsAsync(albumId, cancellationToken);
}
