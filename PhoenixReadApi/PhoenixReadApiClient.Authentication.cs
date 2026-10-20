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

    /// <summary>清除当前客户端保存的登录 Cookie、Token、账号状态和本地会话备份。</summary>
    /// <remarks>不会撤销服务端会话，也不会影响调用方传入的 HttpClient。</remarks>
    public void Logout() => _authentication.Logout();

    /// <summary>释放 API 客户端使用的资源。</summary>
    /// <remarks>
    /// 如果客户端是通过默认构造函数创建的，调用此方法会释放内部 HttpClient；如果传入了外部 HttpClient，
    /// 只释放客户端自身的运行时状态，不会释放外部 HttpClient。重复调用是安全的。
    /// </remarks>
    public void Dispose() => _runtime.Dispose();
}
