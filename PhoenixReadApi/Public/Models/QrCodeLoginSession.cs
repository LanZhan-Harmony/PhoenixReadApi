namespace PhoenixReadApi;

/// <summary>扫码登录会话信息，包含二维码图片与轮询凭证。</summary>
public sealed record QrCodeLoginSession
{
    /// <summary>本次扫码会话的唯一凭证 Token。</summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>二维码图片的 Base64 编码（PNG 格式）。</summary>
    public string QrCodeBase64 { get; init; } = string.Empty;

    /// <summary>二维码图片的二进制原始数据。</summary>
    public byte[] QrCodeImageBytes { get; init; } = [];

    /// <summary>二维码有效秒数。</summary>
    public int ExpiresInSeconds { get; init; }
}

/// <summary>扫码状态检查结果。</summary>
public sealed record QrCodeCheckResult
{
    /// <summary>当前扫码状态。</summary>
    public QrCodeLoginStatus Status { get; init; }

    /// <summary>登录成功时获取到的已认证用户信息；未确认或未登录时为 null。</summary>
    public AuthenticatedUser? User { get; init; }
}
