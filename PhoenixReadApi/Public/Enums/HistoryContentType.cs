namespace PhoenixReadApi;

/// <summary>阅读或观看历史记录的内容类型（对应服务端 book_type）。</summary>
public enum HistoryContentType
{
    /// <summary>小说或漫画（book_type = 0）。</summary>
    Novel = 0,

    /// <summary>有声听书（book_type = 1）。</summary>
    Audiobook = 1,

    /// <summary>普通短剧（book_type = 2）。</summary>
    Drama = 2,

    /// <summary>多季/短视频合集（book_type = 3）。</summary>
    Album = 3,

    /// <summary>独立短视频（book_type = 4）。</summary>
    ShortVideo = 4,

    /// <summary>图文讨论帖子（book_type = 5）。</summary>
    Post = 5,
}
