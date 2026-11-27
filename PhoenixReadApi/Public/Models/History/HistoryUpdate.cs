namespace PhoenixReadApi;

/// <summary>用于粗粒度历史记录上报的条目描述符（对应 read_history/update/v，支持混合多种类型）。</summary>
public sealed class HistoryUpdate
{
    /// <summary>历史记录类型。</summary>
    public required HistoryContentType ContentType { get; init; }

    /// <summary>目标内容 ID（小说/有声书/短剧/合集/短视频/图文 ID）。</summary>
    public required string TargetId { get; init; }

    /// <summary>关联视频 ID（短剧分集、合集子视频或短视频需要；小说/有声书/图文可留空）。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>有声书专用的听书资源 ID（<c>listen_book_id</c>，有声书必填）。</summary>
    public string ListenBookId { get; init; } = string.Empty;

    /// <summary>从 1 开始的集数或子视频序号。</summary>
    public int EpisodeNumber { get; init; }

    /// <summary>从 0 开始的章节序号（小说或有声书使用）。</summary>
    public int ChapterIndex { get; init; }

    /// <summary>当前播放位置（毫秒）。</summary>
    public long CurrentPositionMs { get; init; }

    /// <summary>累计播放时长（毫秒）；未提供时客户端使用当前播放位置。</summary>
    public long AccumulatedPlayTimeMs { get; init; }

    /// <summary>最近阅读或观看时间（Unix 毫秒）；为 0 时默认使用当前时间。</summary>
    public long UpdatedAtUnixMilliseconds { get; init; }

    /// <summary>构造短剧历史记录上报条目。</summary>
    /// <param name="dramaId">短剧 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="episodeNumber">从 1 开始的分集序号。</param>
    /// <param name="currentPositionMs">当前播放位置（毫秒）。</param>
    /// <param name="accumulatedPlayTimeMs">累计播放时长（毫秒）。</param>
    /// <param name="updatedAtUnixMilliseconds">最近观看时间（Unix 毫秒）；0 表示当前时间。</param>
    /// <returns>短剧历史记录上报描述符。</returns>
    public static HistoryUpdate FromDrama(
        string dramaId,
        string videoId,
        int episodeNumber = 1,
        long currentPositionMs = 0,
        long accumulatedPlayTimeMs = 0,
        long updatedAtUnixMilliseconds = 0
    ) =>
        new()
        {
            ContentType = HistoryContentType.Drama,
            TargetId = dramaId,
            VideoId = videoId,
            EpisodeNumber = episodeNumber > 0 ? episodeNumber : 1,
            CurrentPositionMs = Math.Max(0, currentPositionMs),
            AccumulatedPlayTimeMs = Math.Max(0, accumulatedPlayTimeMs),
            UpdatedAtUnixMilliseconds = Math.Max(0, updatedAtUnixMilliseconds),
        };

    /// <summary>构造视频合集历史记录上报条目。</summary>
    /// <param name="albumId">合集 ID。</param>
    /// <param name="videoId">子视频 ID。</param>
    /// <param name="videoIndex">从 1 开始的子视频序号。</param>
    /// <param name="currentPositionMs">当前播放位置（毫秒）。</param>
    /// <param name="accumulatedPlayTimeMs">累计播放时长（毫秒）。</param>
    /// <param name="updatedAtUnixMilliseconds">最近观看时间（Unix 毫秒）；0 表示当前时间。</param>
    /// <returns>合集历史记录上报描述符。</returns>
    public static HistoryUpdate FromVideoAlbum(
        string albumId,
        string videoId,
        int videoIndex = 1,
        long currentPositionMs = 0,
        long accumulatedPlayTimeMs = 0,
        long updatedAtUnixMilliseconds = 0
    ) =>
        new()
        {
            ContentType = HistoryContentType.VideoAlbum,
            TargetId = albumId,
            VideoId = videoId,
            EpisodeNumber = videoIndex > 0 ? videoIndex : 1,
            CurrentPositionMs = Math.Max(0, currentPositionMs),
            AccumulatedPlayTimeMs = Math.Max(0, accumulatedPlayTimeMs),
            UpdatedAtUnixMilliseconds = Math.Max(0, updatedAtUnixMilliseconds),
        };

    /// <summary>构造独立短视频历史记录上报条目。</summary>
    /// <param name="videoId">短视频 ID。</param>
    /// <param name="currentPositionMs">当前播放位置（毫秒）。</param>
    /// <param name="accumulatedPlayTimeMs">累计播放时长（毫秒）。</param>
    /// <param name="updatedAtUnixMilliseconds">最近观看时间（Unix 毫秒）；0 表示当前时间。</param>
    /// <returns>短视频历史记录上报描述符。</returns>
    public static HistoryUpdate FromShortVideo(
        string videoId,
        long currentPositionMs = 0,
        long accumulatedPlayTimeMs = 0,
        long updatedAtUnixMilliseconds = 0
    ) =>
        new()
        {
            ContentType = HistoryContentType.ShortVideo,
            TargetId = videoId,
            VideoId = videoId,
            EpisodeNumber = 1,
            CurrentPositionMs = Math.Max(0, currentPositionMs),
            AccumulatedPlayTimeMs = Math.Max(0, accumulatedPlayTimeMs),
            UpdatedAtUnixMilliseconds = Math.Max(0, updatedAtUnixMilliseconds),
        };

    /// <summary>构造图文帖子浏览历史记录上报条目。</summary>
    /// <param name="postId">图文帖子 ID。</param>
    /// <param name="updatedAtUnixMilliseconds">最近浏览时间（Unix 毫秒）；0 表示当前时间。</param>
    /// <returns>图文帖子历史记录上报描述符。</returns>
    public static HistoryUpdate FromPost(string postId, long updatedAtUnixMilliseconds = 0) =>
        new()
        {
            ContentType = HistoryContentType.Post,
            TargetId = postId,
            UpdatedAtUnixMilliseconds = Math.Max(0, updatedAtUnixMilliseconds),
        };

    /// <summary>构造小说或漫画阅读历史记录上报条目。</summary>
    /// <param name="bookId">小说或漫画 ID。</param>
    /// <param name="chapterIndex">从 0 开始的章节序号。</param>
    /// <param name="updatedAtUnixMilliseconds">最近阅读时间（Unix 毫秒）；0 表示当前时间。</param>
    /// <returns>小说阅读历史记录上报描述符。</returns>
    public static HistoryUpdate FromNovel(
        string bookId,
        int chapterIndex = 0,
        long updatedAtUnixMilliseconds = 0
    ) =>
        new()
        {
            ContentType = HistoryContentType.Novel,
            TargetId = bookId,
            ChapterIndex = Math.Max(0, chapterIndex),
            UpdatedAtUnixMilliseconds = Math.Max(0, updatedAtUnixMilliseconds),
        };

    /// <summary>构造有声听书历史记录上报条目。</summary>
    /// <param name="bookId">书籍主 ID。</param>
    /// <param name="listenBookId">听书资源 ID（<c>listen_book_id</c>）。</param>
    /// <param name="chapterIndex">从 0 开始的章节序号。</param>
    /// <param name="updatedAtUnixMilliseconds">最近收听时间（Unix 毫秒）；0 表示当前时间。</param>
    /// <returns>有声书历史记录上报描述符。</returns>
    public static HistoryUpdate FromAudiobook(
        string bookId,
        string listenBookId,
        int chapterIndex = 0,
        long updatedAtUnixMilliseconds = 0
    ) =>
        new()
        {
            ContentType = HistoryContentType.Audiobook,
            TargetId = bookId,
            ListenBookId = listenBookId,
            ChapterIndex = Math.Max(0, chapterIndex),
            UpdatedAtUnixMilliseconds = Math.Max(0, updatedAtUnixMilliseconds),
        };
}
