namespace PhoenixReadApi;

/// <summary>用户或演职人员主页作品筛选子分类。</summary>
public enum UserWorkSubTab
{
    /// <summary>全部作品。</summary>
    All = 0,

    /// <summary>短剧合集（服务端类型 13）。</summary>
    Collection = 13,

    /// <summary>预约剧集（服务端类型 20）。</summary>
    Reservation = 20,

    /// <summary>剧作（演职人员主创短剧作品，服务端类型 22）。</summary>
    Drama = 22,

    /// <summary>剧评（发表的短剧评价作品，服务端类型 121）。</summary>
    Review = 121,
}
