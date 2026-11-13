namespace PhoenixReadApi;

/// <summary>演员排行榜条目。</summary>
public sealed class ActorLeaderboardItem
{
    /// <summary>榜单名次（从 1 开始）。</summary>
    public int Position { get; init; }

    /// <summary>用户 ID。</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>演员 ID。</summary>
    public string ActorId { get; init; } = string.Empty;

    /// <summary>演员姓名。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>演员头像 URL。</summary>
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>人气热度文案（如“700万人气”）。</summary>
    public string Popularity { get; init; } = string.Empty;

    /// <summary>代表作品。</summary>
    public string RepresentativeWork { get; init; } = string.Empty;

    /// <summary>副标题文案标签（如“10部作品”、“双周追剧200万人”）。</summary>
    public IReadOnlyList<string> SubTitles { get; init; } = [];

    /// <summary>排名变化类型（0: 维持不变, 1: 上升, 2: 下降）。</summary>
    public int RankChangeType { get; init; }

    /// <summary>排名变化数值（上升或下降的名次数）。</summary>
    public int RankChangeValue { get; init; }
}
