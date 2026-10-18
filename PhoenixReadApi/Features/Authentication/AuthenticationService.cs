using System.Text;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Auth;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi.Internal.Features.Authentication;

/// <summary>负责短信登录和会话状态。</summary>
internal sealed class AuthenticationService : PhoenixFeatureService
{
    private AuthenticatedUser _user = new();

    internal AuthenticationService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<VerificationCodeResult> SendVerificationCodeAsync(
        string phone,
        CancellationToken cancellationToken
    )
    {
        string normalized = NormalizePhone(phone);
        AppResponse<AppLoginData> response = await _appTransport
            .PostFormAsync<AppLoginData>(
                "/passport/mobile/send_code/v1/",
                [
                    new("auto_read", "0"),
                    new("account_sdk_source", "app"),
                    new("passport_support_flow", "captcha,verify"),
                    new("unbind_exist", "34"),
                    new("mix_mode", "1"),
                    new("mobile", XorEncode(FormatMobile(normalized))),
                    new("type", XorEncode("24")),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppLoginData data = response.Data ?? new();
        _appTransport.SaveAuthenticationMetadata(data.MobileTicket, data.Mobile, data.RetryTime);
        return new VerificationCodeResult
        {
            Ticket = data.MobileTicket ?? string.Empty,
            RetryAfterSeconds = Math.Max(0, data.RetryTime),
            MaskedPhone = data.Mobile ?? string.Empty,
        };
    }

    internal async Task<AuthenticatedUser> LoginAsync(
        string phone,
        string code,
        CancellationToken cancellationToken
    )
    {
        string normalized = NormalizePhone(phone);
        code = (code ?? string.Empty).Trim();
        if (code.Length is < 4 or > 12 || code.Any(static c => c is < '0' or > '9'))
        {
            throw new ArgumentException("验证码必须是 4 到 12 位数字。", nameof(code));
        }

        AppResponse<AppLoginData> response = await _appTransport
            .PostFormAsync<AppLoginData>(
                "/passport/mobile/sms_login/",
                [
                    new("mobile", XorEncode(FormatMobile(normalized))),
                    new("code", XorEncode(code)),
                    new("account_sdk_source", "app"),
                    new("passport_support_flow", "captcha,verify"),
                    new("mix_mode", "1"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppLoginData data = response.Data ?? new();
        string userId = data.UserId ?? string.Empty;
        if (userId.Length == 0 && data.Mfa is { SmsCodeKey.Length: > 0 })
        {
            throw new PhoenixReadApiProviderException(
                "登录需要额外的人机验证，请在官方客户端完成验证后重试。",
                "/passport/upsms/verify/",
                "MFA",
                isTransient: false
            );
        }
        if (userId.Length == 0)
        {
            throw new PhoenixReadApiProviderException(
                "短信验证码登录未返回账号信息。",
                "/passport/mobile/sms_login/",
                "LOGIN_FAILED",
                isTransient: false
            );
        }

        _user = new AuthenticatedUser
        {
            Id = userId,
            Name = !string.IsNullOrEmpty(data.Name) ? data.Name : data.ScreenName ?? string.Empty,
            AvatarUrl = data.AvatarUrl ?? string.Empty,
            Description = data.Description ?? string.Empty,
            MaskedPhone = data.Mobile ?? string.Empty,
        };
        return _user;
    }

    internal AuthenticationState GetState() =>
        new() { IsAuthenticated = _appTransport.HasSession, User = _user };

    internal void Logout()
    {
        _appTransport.ClearSession();
        _user = new AuthenticatedUser();
    }

    private static string NormalizePhone(string value)
    {
        value = new string(
            (value ?? string.Empty).Where(static c => !char.IsWhiteSpace(c)).ToArray()
        );
        if (value.StartsWith("+86", StringComparison.Ordinal))
        {
            value = value[3..];
        }
        if (value.Length != 11 || value.Any(static c => c is < '0' or > '9'))
        {
            throw new ArgumentException("手机号必须是 11 位中国大陆手机号。", nameof(value));
        }
        return value;
    }

    private static string FormatMobile(string phone) =>
        $"+86{phone[..3]} {phone[3..7]} {phone[7..]}";

    private static string XorEncode(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] ^= 5;
        }
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
