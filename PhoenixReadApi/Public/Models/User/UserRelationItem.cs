namespace PhoenixReadApi;

/// <summary>用户关系列表中的单条用户信息（粉丝或关注用户）。</summary>
public sealed class UserRelationItem
{
    /// <summary>用户身份基本信息（ID、昵称、头像、简介）。</summary>
    public User User { get; init; } = new();

    /// <summary>用户性别（0: 未知/未公开, 1: 男, 2: 女）。</summary>
    public int Gender { get; init; }

    /// <summary>是否为官方认证创作者或蓝V演职人员。</summary>
    public bool IsVerified { get; init; }

    /// <summary>是否为官方入驻演职人员或创作者。</summary>
    public bool IsCreator { get; init; }

    /// <summary>演职人员/主创角色标签（如 ["男演员", "导演"]）。</summary>
    public IReadOnlyList<string> CreatorRoles { get; init; } = [];

    /// <summary>当前客户端或登录账号是否可以关注该用户。</summary>
    public bool CanFollow { get; init; }

    /// <summary>当前登录账号是否已关注该用户。</summary>
    public bool IsFollowing { get; init; }
}
