namespace PhoenixReadApi;

/// <summary>
/// 红果短剧排行榜细分榜单类型。
/// </summary>
public enum ShortDramaLeaderboardBoard
{
    /// <summary>默认榜单（通常为当前内容分类下的推荐榜）。</summary>
    Default = 0,

    /// <summary>推荐榜（热门推荐）。</summary>
    Recommend = 1,

    /// <summary>热播榜（播放量高）。</summary>
    HotPlay = 2,

    /// <summary>新剧榜（最新上线短剧）。</summary>
    NewRelease = 3,

    /// <summary>热搜榜（搜索热度高）。</summary>
    HotSearch = 4,

    /// <summary>必看榜（口碑力荐）。</summary>
    MustWatch = 5,

    /// <summary>收藏榜（追剧收藏最多）。</summary>
    Followed = 6,

    /// <summary>臻果榜（仅全部分类支持）。</summary>
    Prestige = 7,

    /// <summary>预约榜（仅全部分类支持，待播剧预约）。</summary>
    Subscribe = 8,

    /// <summary>追更榜（仅全部分类支持，连载热追）。</summary>
    ChaseUpdate = 9,
}
