namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>向手机号发送短信验证码。</summary>
    /// <param name="phone">中国大陆手机号，可带或不带 <c>+86</c> 和空格。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>短信票据、脱敏手机号和再次发送等待时间。</returns>
    /// <remarks>调用会真实发送短信。登录接口可能进一步要求官方客户端完成人机验证。</remarks>
    public Task<VerificationCodeResult> SendVerificationCodeAsync(
        string phone,
        CancellationToken cancellationToken = default
    ) => _authentication.SendVerificationCodeAsync(phone, cancellationToken);

    /// <summary>使用短信验证码登录。</summary>
    /// <param name="phone">发送验证码时使用的中国大陆手机号。</param>
    /// <param name="code">收到的短信验证码。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>登录后的账号资料；会话 Cookie 保存在当前客户端实例中。</returns>
    /// <remarks>如果服务端要求额外的人机验证，方法会抛出提供方异常，需要先在官方客户端完成验证。</remarks>
    public Task<AuthenticatedUser> LoginAsync(
        string phone,
        string code,
        CancellationToken cancellationToken = default
    ) => _authentication.LoginAsync(phone, code, cancellationToken);

    /// <summary>读取当前客户端的登录状态。</summary>
    /// <returns>是否保存了登录会话以及最近一次登录返回的账号资料。</returns>
    public AuthenticationState GetAuthenticationState() => _authentication.GetState();

    /// <summary>获取当前已登录账号的详细资料与 VIP 信息。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>当前已登录账号的资料与 VIP 状态。</returns>
    public Task<CurrentUserInfo> GetCurrentUserInfoAsync(
        CancellationToken cancellationToken = default
    ) => _authentication.GetCurrentUserInfoAsync(cancellationToken);

    /// <summary>主动向红果验证当前保存的会话是否仍然有效；若已在服务端过期则自动清除并重置为未登录。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>若会话在服务端仍有效返回 true；未登录或已过期返回 false。</returns>
    public Task<bool> ValidateSessionAsync(
        CancellationToken cancellationToken = default
    ) => _authentication.ValidateSessionAsync(cancellationToken);

    /// <summary>发起网页扫码登录，获取用于展示的二维码图片与轮询凭证。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含二维码图片数据、有效期和凭证 Token 的扫码会话。</returns>
    public Task<QrCodeLoginSession> CreateQrCodeLoginAsync(
        CancellationToken cancellationToken = default
    ) => _authentication.CreateQrCodeLoginAsync(cancellationToken);

    /// <summary>检查网页扫码登录的确认状态。</summary>
    /// <param name="token">由 <see cref="CreateQrCodeLoginAsync"/> 返回的凭证 Token。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>当前扫码状态；确认登录成功时同时返回已认证用户资料并写入客户端会话。</returns>
    public Task<QrCodeCheckResult> CheckQrCodeStatusAsync(
        string token,
        CancellationToken cancellationToken = default
    ) => _authentication.CheckQrCodeStatusAsync(token, cancellationToken);

    /// <summary>清除当前客户端保存的登录 Cookie、Token、账号状态和本地会话备份。</summary>
    /// <remarks>不会撤销服务端会话，也不会影响调用方传入的 HttpClient。</remarks>
    public void Logout() => _authentication.Logout();
}
