namespace PhoenixReadApi;

/// <summary>发送短信验证码后的状态。</summary>
public sealed class VerificationCodeResult
{
    /// <summary>服务端用于后续验证的票据。</summary>
    public string Ticket { get; init; } = string.Empty;

    /// <summary>再次发送前应等待的秒数。</summary>
    public int RetryAfterSeconds { get; init; }

    /// <summary>脱敏后的手机号。</summary>
    public string MaskedPhone { get; init; } = string.Empty;
}

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
}

/// <summary>当前客户端登录状态。</summary>
public sealed class AuthenticationState
{
    /// <summary>是否已经保存登录 Cookie。</summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>当前账号资料；未登录时为空对象。</summary>
    public AuthenticatedUser User { get; init; } = new();
}
