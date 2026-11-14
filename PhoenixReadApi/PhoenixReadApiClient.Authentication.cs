namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取登录支持的国家或地区区号列表。</summary>
    /// <returns>包含地区名称、国际区号及 ISO 代码的地区列表。</returns>
    public IReadOnlyList<LoginRegion> GetLoginRegions() => _authentication.GetLoginRegions();

    /// <summary>异步获取登录支持的国家或地区区号列表。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含地区名称、国际区号及 ISO 代码的地区列表。</returns>
    public Task<IReadOnlyList<LoginRegion>> GetLoginRegionsAsync(
        CancellationToken cancellationToken = default
    ) => _authentication.GetLoginRegionsAsync(cancellationToken);

    /// <summary>发送短信验证码。</summary>
    /// <param name="phoneNumber">接收验证码的中国大陆手机号（11 位数字）。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含重发等待时间及脱敏手机号的发送结果。</returns>
    public Task<VerificationCodeResult> SendVerificationCodeAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default
    ) => _authentication.SendVerificationCodeAsync(phoneNumber, cancellationToken);

    /// <summary>使用手机号和收到的短信验证码登录。</summary>
    /// <param name="phoneNumber">已接收验证码的手机号。</param>
    /// <param name="code">收到的 4-6 位短信验证码。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>登录成功后的已认证用户信息。</returns>
    public Task<AuthenticatedUser> LoginAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default
    ) => _authentication.LoginAsync(phoneNumber, code, cancellationToken);

    /// <summary>读取当前客户端的登录状态。</summary>
    /// <returns>是否保存了登录会话以及最近一次登录返回的账号资料。</returns>
    public AuthenticationState GetAuthenticationState() => _authentication.GetState();

    /// <summary>查询当前登录用户在红果服务端的最新资料。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>用户基础信息及 VIP 状态。</returns>
    public Task<AuthenticatedUser> GetCurrentUserInfoAsync(
        CancellationToken cancellationToken = default
    ) => _authentication.GetCurrentUserInfoAsync(cancellationToken);

    /// <summary>检查当前会话凭据是否在红果服务端仍然有效。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>有效返回 true；已失效或未登录返回 false。</returns>
    public Task<bool> ValidateSessionAsync(CancellationToken cancellationToken = default) =>
        _authentication.ValidateSessionAsync(cancellationToken);

    /// <summary>发起网页扫码登录会话，获取供终端用户扫描的二维码图片及凭证。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>包含二维码图片字节、Base64 字符串及轮询 Token 的登录会话。</returns>
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

    /// <summary>显式重置并重新注册匿名设备标识（当遭遇服务端匿名播放次数限制时可使用）。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    public Task ResetDeviceAsync(CancellationToken cancellationToken = default) =>
        _runtime.AppTransport.ResetDeviceAsync(cancellationToken);
}
