namespace PhoenixReadApi;

/// <summary>登录后的账号资料。</summary>
public sealed class AuthenticatedUser
{
    /// <summary>账号 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>账号名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>头像 URL。</summary>
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>个人简介。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>脱敏手机号。</summary>
    public string MaskedPhone { get; init; } = string.Empty;

    /// <summary>性别：0 为未知，1 为男，2 为女。</summary>
    public int Gender { get; init; }

    /// <summary>是否为 VIP 会员。</summary>
    public bool IsVip { get; init; }
}
