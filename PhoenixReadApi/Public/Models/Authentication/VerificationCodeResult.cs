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
