namespace PhoenixReadApi;

/// <summary>扫码状态检查结果。</summary>
public sealed class QrCodeCheckResult
{
    /// <summary>当前扫码状态。</summary>
    public QrCodeLoginStatus Status { get; init; }

    /// <summary>登录成功时获取到的已认证用户信息；未确认或未登录时为 null。</summary>
    public AuthenticatedUser? User { get; init; }
}
