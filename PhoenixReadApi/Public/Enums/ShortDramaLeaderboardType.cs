namespace PhoenixReadApi;

/// <summary>
/// 红果短剧排行榜一级榜单类型。
/// </summary>
public enum ShortDramaLeaderboardType
{
    /// <summary>推荐榜（热门推荐，基于观看、互动及兴趣排序，默认）。</summary>
    Recommend = 0,

    /// <summary>热播榜（播放热度高，基于前一日播放热度排序）。</summary>
    HotPlay = 1,

    /// <summary>新剧榜（新上线短剧的初期综合热度排序）。</summary>
    NewRelease = 2,

    /// <summary>热搜榜（搜索热度高，基于前一日搜索热度排序）。</summary>
    HotSearch = 3,
}
