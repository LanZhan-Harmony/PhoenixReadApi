namespace PhoenixReadApi;

/// <summary>用于细粒度阅读或播放进度同步的条目描述符（对应 read_progress/upload/v）。</summary>
public sealed class ProgressUpdate
{
    /// <summary>内容类型。</summary>
    public required HistoryContentType ContentType { get; init; }

    /// <summary>目标内容 ID（小说/短剧/合集/短视频 ID，或有声书的 <c>listen_book_id</c>）。</summary>
    public required string TargetId { get; init; }

    /// <summary>子条目 ID（小说章节 ID、有声书音频章节 ID、短剧分集 ID、合集子视频 ID 或短视频 ID）。</summary>
    public string ItemId { get; init; } = string.Empty;

    /// <summary>当前播放时长（毫秒）。</summary>
    public long CurrentPlayTimeMs { get; init; }

    /// <summary>播放器累计总时长（毫秒）。</summary>
    public long CumulativeTotalDurationMs { get; init; }

    /// <summary>视频索引（默认 0）。</summary>
    public int VideoIndex { get; init; }

    /// <summary>页码索引（小说使用，从 0 开始）。</summary>
    public int PageIndex { get; init; }

    /// <summary>页内阅读进度比例（0.0 到 1.0）。</summary>
    public float PageProgressRate { get; init; }

    /// <summary>当前章节进度百分比字符串（如 <c>"26.1334"</c>）。</summary>
    public string? ItemProgressRate { get; init; }

    /// <summary>全书或整部作品进度比例字符串（如 <c>"0.0019"</c>）。</summary>
    public string? ProgressRate { get; init; }

    /// <summary>段落 ID。</summary>
    public string? ParagraphId { get; init; }

    /// <summary>段落内偏移量。</summary>
    public int ParagraphOffset { get; init; }

    /// <summary>精细段落定位信息（对应 <c>progress_pos_info_v2</c>）。</summary>
    public ReadingPosition? Position { get; init; }

    /// <summary>频道 ID。</summary>
    public long ChannelId { get; init; }

    /// <summary>当前频道 ID。</summary>
    public long CurrentChannelId { get; init; }

    /// <summary>体裁类型。</summary>
    public int GenreType { get; init; }

    /// <summary>音色 ID（小说听书等场景使用，默认 0，小说阅读为 -1）。</summary>
    public int ToneId { get; init; }

    /// <summary>最近阅读或播放时间（Unix 毫秒）；为 0 时默认使用当前时间。</summary>
    public long UpdatedAtUnixMilliseconds { get; init; }

    /// <summary>构造小说精细阅读进度上报条目。</summary>
    /// <param name="bookId">小说 ID。</param>
    /// <param name="chapterId">章节 ID（<c>item_id</c>）。</param>
    /// <param name="pageIndex">章节内页码索引。</param>
    /// <param name="pageProgressRate">页内阅读进度比例。</param>
    /// <param name="chapterProgressRate">当前章节进度百分比字符串。</param>
    /// <param name="bookProgressRate">全书阅读进度比例字符串。</param>
    /// <param name="paragraphId">段落 ID。</param>
    /// <param name="paragraphOffset">段落内偏移量。</param>
    /// <param name="position">精细段落定位信息。</param>
    /// <param name="channelId">频道 ID。</param>
    /// <param name="genreType">体裁类型。</param>
    /// <param name="toneId">音色 ID。</param>
    /// <param name="updatedAtUnixMilliseconds">最近阅读时间（Unix 毫秒）；0 表示当前时间。</param>
    /// <returns>小说精细进度上报描述符。</returns>
    public static ProgressUpdate FromNovel(
        string bookId,
        string chapterId,
        int pageIndex = 0,
        float pageProgressRate = 0,
        string? chapterProgressRate = null,
        string? bookProgressRate = null,
        string paragraphId = "0",
        int paragraphOffset = 0,
        ReadingPosition? position = null,
        long channelId = 0,
        int genreType = 0,
        int toneId = -1,
        long updatedAtUnixMilliseconds = 0
    ) =>
        new()
        {
            ContentType = HistoryContentType.Novel,
            TargetId = bookId,
            ItemId = chapterId,
            PageIndex = Math.Max(0, pageIndex),
            PageProgressRate = pageProgressRate,
            ItemProgressRate = chapterProgressRate,
            ProgressRate = bookProgressRate,
            ParagraphId = paragraphId,
            ParagraphOffset = Math.Max(0, paragraphOffset),
            Position = position,
            ChannelId = channelId,
            GenreType = genreType,
            ToneId = toneId,
            UpdatedAtUnixMilliseconds = Math.Max(0, updatedAtUnixMilliseconds),
        };

    /// <summary>构造有声书精细播放进度上报条目。</summary>
    /// <param name="listenBookId">听书资源 ID（抓包中该接口的 <c>book_id</c> 使用 <c>listen_book_id</c>）。</param>
    /// <param name="chapterId">音频章节 ID（<c>item_id</c>）。</param>
    /// <param name="currentPlayTimeMs">当前播放时长（毫秒）。</param>
    /// <param name="bookProgressRate">全书进度比例字符串。</param>
    /// <param name="chapterProgressRate">当前章节进度字符串。</param>
    /// <param name="channelId">频道 ID。</param>
    /// <param name="genreType">体裁类型。</param>
    /// <param name="toneId">音色 ID。</param>
    /// <param name="updatedAtUnixMilliseconds">最近收听时间（Unix 毫秒）；0 表示当前时间。</param>
    /// <returns>有声书精细进度上报描述符。</returns>
    public static ProgressUpdate FromAudiobook(
        string listenBookId,
        string chapterId,
        long currentPlayTimeMs = 0,
        string? bookProgressRate = null,
        string? chapterProgressRate = null,
        long channelId = 0,
        int genreType = 0,
        int toneId = 0,
        long updatedAtUnixMilliseconds = 0
    ) =>
        new()
        {
            ContentType = HistoryContentType.Audiobook,
            TargetId = listenBookId,
            ItemId = chapterId,
            CurrentPlayTimeMs = Math.Max(0, currentPlayTimeMs),
            ProgressRate = bookProgressRate ?? "0",
            ItemProgressRate = chapterProgressRate ?? "0",
            ParagraphId = "-1",
            Position = new ReadingPosition
            {
                StartContainerIndex = -1,
                StartElementIndex = -1,
                StartElementOffset = -1,
            },
            ChannelId = channelId,
            GenreType = genreType,
            ToneId = toneId,
            UpdatedAtUnixMilliseconds = Math.Max(0, updatedAtUnixMilliseconds),
        };

    /// <summary>构造短剧精细播放进度上报条目。</summary>
    /// <param name="dramaId">短剧 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="currentPlayTimeMs">当前播放时长（毫秒）。</param>
    /// <param name="cumulativeTotalDurationMs">播放器累计总时长（毫秒）。</param>
    /// <param name="videoIndex">视频索引。</param>
    /// <param name="updatedAtUnixMilliseconds">最近观看时间（Unix 毫秒）；0 表示当前时间。</param>
    /// <returns>短剧精细进度上报描述符。</returns>
    public static ProgressUpdate FromDrama(
        string dramaId,
        string videoId,
        long currentPlayTimeMs = 0,
        long cumulativeTotalDurationMs = 0,
        int videoIndex = 0,
        long updatedAtUnixMilliseconds = 0
    ) =>
        new()
        {
            ContentType = HistoryContentType.Drama,
            TargetId = dramaId,
            ItemId = videoId,
            CurrentPlayTimeMs = Math.Max(0, currentPlayTimeMs),
            CumulativeTotalDurationMs = Math.Max(0, cumulativeTotalDurationMs),
            VideoIndex = Math.Max(0, videoIndex),
            UpdatedAtUnixMilliseconds = Math.Max(0, updatedAtUnixMilliseconds),
        };

    /// <summary>构造视频合集精细播放进度上报条目。</summary>
    /// <param name="albumId">合集 ID。</param>
    /// <param name="videoId">子视频 ID。</param>
    /// <param name="currentPlayTimeMs">当前播放时长（毫秒）。</param>
    /// <param name="cumulativeTotalDurationMs">播放器累计总时长（毫秒）。</param>
    /// <param name="videoIndex">视频索引。</param>
    /// <param name="updatedAtUnixMilliseconds">最近观看时间（Unix 毫秒）；0 表示当前时间。</param>
    /// <returns>合集精细进度上报描述符。</returns>
    public static ProgressUpdate FromVideoAlbum(
        string albumId,
        string videoId,
        long currentPlayTimeMs = 0,
        long cumulativeTotalDurationMs = 0,
        int videoIndex = 0,
        long updatedAtUnixMilliseconds = 0
    ) =>
        new()
        {
            ContentType = HistoryContentType.VideoAlbum,
            TargetId = albumId,
            ItemId = videoId,
            CurrentPlayTimeMs = Math.Max(0, currentPlayTimeMs),
            CumulativeTotalDurationMs = Math.Max(0, cumulativeTotalDurationMs),
            VideoIndex = Math.Max(0, videoIndex),
            UpdatedAtUnixMilliseconds = Math.Max(0, updatedAtUnixMilliseconds),
        };

    /// <summary>构造独立短视频精细播放进度上报条目。</summary>
    /// <param name="videoId">短视频 ID。</param>
    /// <param name="currentPlayTimeMs">当前播放时长（毫秒）。</param>
    /// <param name="cumulativeTotalDurationMs">播放器累计总时长（毫秒）。</param>
    /// <param name="videoIndex">视频索引。</param>
    /// <param name="updatedAtUnixMilliseconds">最近观看时间（Unix 毫秒）；0 表示当前时间。</param>
    /// <returns>短视频精细进度上报描述符。</returns>
    public static ProgressUpdate FromShortVideo(
        string videoId,
        long currentPlayTimeMs = 0,
        long cumulativeTotalDurationMs = 0,
        int videoIndex = 0,
        long updatedAtUnixMilliseconds = 0
    ) =>
        new()
        {
            ContentType = HistoryContentType.ShortVideo,
            TargetId = videoId,
            ItemId = videoId,
            CurrentPlayTimeMs = Math.Max(0, currentPlayTimeMs),
            CumulativeTotalDurationMs = Math.Max(0, cumulativeTotalDurationMs),
            VideoIndex = Math.Max(0, videoIndex),
            UpdatedAtUnixMilliseconds = Math.Max(0, updatedAtUnixMilliseconds),
        };
}
