namespace PhoenixReadApi;

/// <summary>
/// 红果短剧排行榜内容分类。
/// </summary>
public enum ShortDramaLeaderboardCategory
{
    /// <summary>全部内容短剧榜（默认）。</summary>
    All = 0,

    /// <summary>真人短剧榜单。</summary>
    Human = 1,

    /// <summary>漫剧榜单。</summary>
    Comic = 2,

    /// <summary>AI 短剧榜单。</summary>
    Ai = 3,

    /// <summary>系列剧榜单。</summary>
    SeriesAlbum = 4,
}
