namespace PhoenixReadApi;

/// <summary>收藏或取消收藏操作的目标描述符（支持短剧、视频合集、短视频、图文帖子、有声书与小说）。</summary>
public sealed class FavoriteTarget
{
    /// <summary>目标内容类型。</summary>
    public required HistoryContentType ContentType { get; init; }

    /// <summary>目标内容 ID（短剧/合集/短视频/图文/有声书/小说 ID）。</summary>
    public required string TargetId { get; init; }

    /// <summary>分集视频 ID（仅短剧收藏定位需要）。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>构造短剧收藏目标。</summary>
    /// <param name="dramaId">短剧 ID。</param>
    /// <param name="videoId">用于定位收藏记录的分集视频 ID。</param>
    /// <returns>短剧收藏目标描述符。</returns>
    public static FavoriteTarget FromDrama(string dramaId, string videoId) =>
        new()
        {
            ContentType = HistoryContentType.Drama,
            TargetId = dramaId,
            VideoId = videoId,
        };

    /// <summary>构造视频合集收藏目标。</summary>
    /// <param name="albumId">视频合集 ID，必须是数字字符串。</param>
    /// <returns>视频合集收藏目标描述符。</returns>
    public static FavoriteTarget FromVideoAlbum(string albumId) =>
        new() { ContentType = HistoryContentType.VideoAlbum, TargetId = albumId };

    /// <summary>构造独立短视频收藏目标。</summary>
    /// <param name="videoId">短视频 ID，必须是数字字符串。</param>
    /// <returns>独立短视频收藏目标描述符。</returns>
    public static FavoriteTarget FromShortVideo(string videoId) =>
        new() { ContentType = HistoryContentType.ShortVideo, TargetId = videoId };

    /// <summary>构造图文帖子收藏目标。</summary>
    /// <param name="postId">图文帖子 ID，必须是数字字符串。</param>
    /// <returns>图文帖子收藏目标描述符。</returns>
    public static FavoriteTarget FromPost(string postId) =>
        new() { ContentType = HistoryContentType.Post, TargetId = postId };

    /// <summary>构造有声听书书架收藏目标。</summary>
    /// <param name="bookId">书籍 ID 或听书资源 ID（<c>listen_book_id</c>）。</param>
    /// <returns>有声书收藏目标描述符。</returns>
    public static FavoriteTarget FromAudiobook(string bookId) =>
        new() { ContentType = HistoryContentType.Audiobook, TargetId = bookId };

    /// <summary>构造小说或漫画书架收藏目标。</summary>
    /// <param name="bookId">小说或漫画 ID。</param>
    /// <returns>小说收藏目标描述符。</returns>
    public static FavoriteTarget FromNovel(string bookId) =>
        new() { ContentType = HistoryContentType.Novel, TargetId = bookId };
}
