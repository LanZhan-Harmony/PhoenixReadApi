namespace PhoenixReadApi;

/// <summary>综合搜索结果项（覆盖短剧、合集、短视频、小说、用户、图文讨论与有声听书的联合类型）。</summary>
public readonly union ComprehensiveSearchItem(
    Drama,
    VideoAlbum,
    ShortVideo,
    Novel,
    User,
    Post,
    Audiobook
);
