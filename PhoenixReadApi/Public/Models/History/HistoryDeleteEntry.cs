namespace PhoenixReadApi;

/// <summary>用于批量删除历史记录的条目描述符（支持混合删除多种类型）。</summary>
public sealed class HistoryDeleteEntry
{
    /// <summary>历史记录类型。</summary>
    public required HistoryContentType ContentType { get; init; }

    /// <summary>目标内容 ID（小说/有声书/短剧/合集/短视频/图文 ID）。</summary>
    public required string TargetId { get; init; }

    /// <summary>关联视频 ID（短剧分集、合集子视频或短视频需要；小说/有声书/图文可留空）。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>有声书专用的听书资源 ID（<c>listen_book_id</c>，若有）。</summary>
    public string ListenBookId { get; init; } = string.Empty;

    /// <summary>集数或子视频序号（视频类默认为 1，其他为 0）。</summary>
    public int EpisodeNumber { get; init; }

    /// <summary>构造删除短剧历史的条目。</summary>
    public static HistoryDeleteEntry FromDrama(
        string dramaId,
        string videoId,
        int episodeNumber = 1
    ) =>
        new()
        {
            ContentType = HistoryContentType.Drama,
            TargetId = dramaId,
            VideoId = videoId,
            EpisodeNumber = episodeNumber > 0 ? episodeNumber : 1,
        };

    /// <summary>构造删除合集历史的条目。</summary>
    public static HistoryDeleteEntry FromVideoAlbum(
        string albumId,
        string videoId = "",
        int episodeNumber = 1
    ) =>
        new()
        {
            ContentType = HistoryContentType.VideoAlbum,
            TargetId = albumId,
            VideoId = videoId,
            EpisodeNumber = episodeNumber > 0 ? episodeNumber : 1,
        };

    /// <summary>构造删除独立短视频历史的条目。</summary>
    public static HistoryDeleteEntry FromShortVideo(string videoId) =>
        new()
        {
            ContentType = HistoryContentType.ShortVideo,
            TargetId = videoId,
            VideoId = videoId,
            EpisodeNumber = 1,
        };

    /// <summary>构造删除图文帖子历史的条目。</summary>
    public static HistoryDeleteEntry FromPost(string postId) =>
        new() { ContentType = HistoryContentType.Post, TargetId = postId };

    /// <summary>构造删除小说或漫画历史的条目。</summary>
    public static HistoryDeleteEntry FromNovel(string bookId) =>
        new() { ContentType = HistoryContentType.Novel, TargetId = bookId };

    /// <summary>构造删除有声听书历史的条目。</summary>
    public static HistoryDeleteEntry FromAudiobook(string bookId, string listenBookId = "") =>
        new()
        {
            ContentType = HistoryContentType.Audiobook,
            TargetId = bookId,
            ListenBookId = listenBookId,
        };

    /// <summary>从强类型历史记录项直接构造删除条目。</summary>
    public static HistoryDeleteEntry FromItem<T>(HistoryItem<T> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Target switch
        {
            Drama d => FromDrama(d.Id, item.VideoId, item.EpisodeNumber),
            VideoAlbum a => FromVideoAlbum(a.Id, item.VideoId, item.EpisodeNumber),
            ShortVideo s => FromShortVideo(
                !string.IsNullOrWhiteSpace(item.VideoId) ? item.VideoId : s.VideoId
            ),
            Post p => FromPost(p.Id),
            Novel n => FromNovel(n.Id),
            Audiobook ab => FromAudiobook(ab.Id, ab.ListenBookId),
            _ => throw new NotSupportedException($"不支持的历史记录目标类型: {typeof(T).Name}"),
        };
    }
}
