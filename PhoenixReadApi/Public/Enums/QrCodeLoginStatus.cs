namespace PhoenixReadApi;

/// <summary>网页扫码登录的状态。</summary>
public enum QrCodeLoginStatus
{
    /// <summary>等待扫码。</summary>
    WaitingForScan,

    /// <summary>已扫码，等待手机端确认。</summary>
    Scanned,

    /// <summary>已确认登录并成功建立会话。</summary>
    Confirmed,

    /// <summary>二维码已过期，需要重新获取。</summary>
    Expired,

    /// <summary>扫码校验失败。</summary>
    Failed,
}
