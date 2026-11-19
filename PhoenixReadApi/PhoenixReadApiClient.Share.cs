using PhoenixReadApi.Internal.Features.Share;

namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>创建剧集分集分享链接。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端返回的分享 URL 和关联 ID。</returns>
    /// <remarks>部分节点暂未开放短链接生成；这种情况下仍返回 DramaId 和 VideoId，Url 与 ShortUrl 为空。</remarks>
    public Task<ShareLink> CreateShareLinkAsync(
        string dramaId,
        string videoId,
        CancellationToken cancellationToken = default
    ) => _share.CreateShareLinkAsync(dramaId, videoId, cancellationToken);

    /// <summary>从分享 URL 中解析剧集和分集 ID。</summary>
    /// <param name="url">完整分享 URL。</param>
    /// <returns>解析出的分享目标；链接不含某个 ID 时对应属性为空字符串。</returns>
    public ShareLink ResolveShareLink(string url) => ShareService.ResolveShareLink(url);
}
