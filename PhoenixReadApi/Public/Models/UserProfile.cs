namespace PhoenixReadApi;

/// <summary>账号主页及其公开内容。</summary>
public sealed class UserProfile
{
    /// <summary>账号身份信息。</summary>
    public User User { get; init; } = new();

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

    /// <summary>账号作为演员、导演、编剧或出品方关联的剧集。</summary>
    public IReadOnlyList<Drama> RelatedDramas { get; init; } = [];

    /// <summary>关联剧集列表的分页状态。</summary>
    public UserPageInfo RelatedDramasPage { get; init; } = new();
}
