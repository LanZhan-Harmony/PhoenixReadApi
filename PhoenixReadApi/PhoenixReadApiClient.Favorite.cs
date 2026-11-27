namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取当前客户端账号书架中收藏的小说列表（服务端 book_type=0，全量返回）。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏的小说列表。</returns>
    public Task<IReadOnlyList<Novel>> GetNovelFavoritesAsync(
        CancellationToken cancellationToken = default
    ) => _favorite.GetNovelFavoritesAsync(cancellationToken);

    /// <summary>获取当前客户端账号书架中收藏的有声听书列表（服务端 book_type=1，全量返回）。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏的有声听书列表。</returns>
    public Task<IReadOnlyList<Audiobook>> GetAudiobookFavoritesAsync(
        CancellationToken cancellationToken = default
    ) => _favorite.GetAudiobookFavoritesAsync(cancellationToken);

    /// <summary>获取当前客户端账号收藏的短剧列表（服务端 book_type=2，全量返回）。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏的短剧列表。</returns>
    /// <remarks>当前 App 视频书架列表只返回作品 ID；如需完整元数据请调用 <see cref="GetDramaDetailsAsync"/>。</remarks>
    public Task<IReadOnlyList<Drama>> GetDramaFavoritesAsync(
        CancellationToken cancellationToken = default
    ) => _favorite.GetDramaFavoritesAsync(cancellationToken);

    /// <summary>获取当前客户端账号收藏的视频合集列表（服务端 book_type=3，全量返回）。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏的视频合集列表。</returns>
    public Task<IReadOnlyList<DramaAlbum>> GetAlbumFavoritesAsync(
        CancellationToken cancellationToken = default
    ) => _favorite.GetAlbumFavoritesAsync(cancellationToken);

    /// <summary>获取当前客户端账号收藏的独立短视频列表（服务端 book_type=4，全量返回）。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏的独立短视频列表。</returns>
    public Task<IReadOnlyList<ShortVideo>> GetShortVideoFavoritesAsync(
        CancellationToken cancellationToken = default
    ) => _favorite.GetShortVideoFavoritesAsync(cancellationToken);

    /// <summary>获取当前客户端账号收藏的图文帖子列表。</summary>
    /// <param name="count">本页数量，范围为 1 到 50。</param>
    /// <param name="offset">偏移量；首次请求使用 0，后续使用上一页的 NextOffset。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>收藏的图文帖子及分页状态。</returns>
    public Task<PostFavoritePage> GetPostFavoritesAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    ) => _favorite.GetPostFavoritesAsync(count, offset, cancellationToken);

    /// <summary>收藏或取消收藏一部剧。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">用于定位收藏记录的分集视频 ID。</param>
    /// <param name="active">true 表示收藏，false 表示取消收藏。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetFavoriteAsync(
        string dramaId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _favorite.SetFavoriteAsync(dramaId, videoId, active, cancellationToken);

    /// <summary>收藏或取消收藏一个独立短视频。</summary>
    /// <param name="videoId">短视频 ID，必须是数字字符串。</param>
    /// <param name="active">true 表示收藏，false 表示取消收藏。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetShortVideoFavoriteAsync(
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _favorite.SetShortVideoFavoriteAsync(videoId, active, cancellationToken);

    /// <summary>收藏或取消收藏一个视频合集。</summary>
    /// <param name="albumId">视频合集 ID，必须是数字字符串。</param>
    /// <param name="active">true 表示收藏，false 表示取消收藏。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetDramaAlbumFavoriteAsync(
        string albumId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _favorite.SetDramaAlbumFavoriteAsync(albumId, active, cancellationToken);

    /// <summary>收藏或取消收藏一篇图文帖子。</summary>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <param name="active">true 表示收藏，false 表示取消收藏。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetPostFavoriteAsync(
        string postId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) => _favorite.SetPostFavoriteAsync(postId, active, cancellationToken);
}
