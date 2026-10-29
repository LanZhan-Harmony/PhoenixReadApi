namespace PhoenixReadApi;

/// <summary>当前已登录账号的资料与配置信息。</summary>
public sealed record CurrentUserInfo
{
    /// <summary>账号数字 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>用户昵称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>头像 URL。</summary>
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>个性签名 / 简介。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>性别：0 为未知，1 为男，2 为女。</summary>
    public int Gender { get; init; }

    /// <summary>是否为 VIP 会员。</summary>
    public bool IsVip { get; init; }
}
