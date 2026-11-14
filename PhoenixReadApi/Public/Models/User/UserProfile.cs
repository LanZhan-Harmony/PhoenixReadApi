namespace PhoenixReadApi;

/// <summary>账号主页及其公开内容与创作者/演职人员资料。</summary>
public sealed class UserProfile
{
    /// <summary>账号身份信息。</summary>
    public User User { get; init; } = new();

    /// <summary>红果号（如 "Huangyankia"）。</summary>
    public string HongguoId { get; init; } = string.Empty;

    /// <summary>是否为蓝V认证演职人员或官方认证创作者。</summary>
    public bool IsVerified { get; init; }

    /// <summary>是否为官方入驻演职人员或创作者（演员、导演、编剧、制作人等）。</summary>
    public bool IsCreator { get; init; }

    /// <summary>演职人员/主创身份角色标签列表（如 ["男演员", "导演"]）。</summary>
    public IReadOnlyList<string> CreatorRoles { get; init; } = [];

    /// <summary>入驻与称号徽章列表（如 ["已入驻"]）。</summary>
    public IReadOnlyList<string> Badges { get; init; } = [];

    /// <summary>成就荣誉勋章列表（如“10亿播放剧主演”）。</summary>
    public IReadOnlyList<UserHonor> Honors { get; init; } = [];

    /// <summary>粉丝总数。</summary>
    public long FansCount { get; init; }

    /// <summary>关注总数。</summary>
    public long FollowCount { get; init; }

    /// <summary>获赞总数。</summary>
    public long LikeCount { get; init; }

    /// <summary>当前客户端或登录账号是否可以关注该用户。</summary>
    public bool CanFollow { get; init; }

    /// <summary>当前登录账号是否已关注该用户。</summary>
    public bool IsFollowing { get; init; }

    /// <summary>主页顶部背景封面图 URL。</summary>
    public string BackgroundCoverUrl { get; init; } = string.Empty;

    /// <summary>账号主页作品（短剧作品列表，优先加载第 16 号作品 Tab）。</summary>
    public IReadOnlyList<Drama> Works { get; init; } = [];

    /// <summary>主页作品列表的分页状态。</summary>
    public UserWorkPage WorksPage { get; init; } = new();

    /// <summary>账号收藏的剧集。</summary>
    public IReadOnlyList<Drama> Collections { get; init; } = [];

    /// <summary>收藏列表的分页状态。</summary>
    public UserPageInfo CollectionsPage { get; init; } = new();

    /// <summary>账号点赞过的剧集。</summary>
    public IReadOnlyList<Drama> Likes { get; init; } = [];

    /// <summary>点赞列表的分页状态。</summary>
    public UserPageInfo LikesPage { get; init; } = new();

    /// <summary>账号发布的动态。</summary>
    public IReadOnlyList<UserActivity> Activities { get; init; } = [];

    /// <summary>动态列表的分页状态。</summary>
    public UserPageInfo ActivitiesPage { get; init; } = new();

    /// <summary>主页支持的全部内容 Tab 分类列表。</summary>
    public IReadOnlyList<UserProfileTab> AvailableTabs { get; init; } = [];
}
