namespace PhoenixReadApi;

/// <summary>当前客户端登录状态。</summary>
public sealed class AuthenticationState
{
    /// <summary>是否已经保存登录 Cookie。</summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>当前账号资料；未登录时为空对象。</summary>
    public AuthenticatedUser User { get; init; } = new();
}