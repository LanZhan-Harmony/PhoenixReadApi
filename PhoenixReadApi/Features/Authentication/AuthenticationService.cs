using System.Globalization;
using System.Text;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Auth;
using PhoenixReadApi.Internal.Contracts.App.Users;
using PhoenixReadApi.Internal.Contracts.Web.Auth;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi.Internal.Features.Authentication;

/// <summary>负责短信登录和会话状态。</summary>
internal sealed class AuthenticationService : PhoenixFeatureService
{
    private AuthenticatedUser _user = new();

    internal AuthenticationService(PhoenixReadClientRuntime runtime)
        : base(runtime)
    {
        _user = _appTransport.RestoredUser;
    }

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
        _appTransport.SaveAuthenticatedUser(_user);
        return _user;
    }

    internal AuthenticationState GetState() =>
        new() { IsAuthenticated = _appTransport.HasSession, User = _user };

    internal void Logout()
    {
        _appTransport.ClearSession();
        _user = new AuthenticatedUser();
    }

    internal async Task<CurrentUserInfo> GetCurrentUserInfoAsync(
        CancellationToken cancellationToken
    )
    {
        AppResponse<AppCurrentUserInfoData> response = await _appTransport
            .GetAsync<AppCurrentUserInfoData>("/reading/user/info/v1/", [], cancellationToken)
            .ConfigureAwait(false);
        AppCurrentUserInfoData data = response.Data ?? new();
        string userId = !string.IsNullOrEmpty(data.UserId) ? data.UserId : data.BizUserId;
        CurrentUserInfo info = new()
        {
            Id = userId,
            Name = data.UserName,
            AvatarUrl = data.AvatarUrl,
            Description = data.Description,
            Gender = data.Gender,
            IsVip = data.VipInfo.IsVip == "1",
        };

        if (userId.Length > 0)
        {
            _user = new AuthenticatedUser
            {
                Id = info.Id,
                Name = info.Name,
                AvatarUrl = info.AvatarUrl,
                Description = info.Description,
                MaskedPhone = _user.MaskedPhone,
            };
            _appTransport.SaveAuthenticatedUser(_user);
        }

        return info;
    }

    internal async Task<bool> ValidateSessionAsync(CancellationToken cancellationToken)
    {
        if (!_appTransport.HasSession)
        {
            return false;
        }

        try
        {
            CurrentUserInfo info = await GetCurrentUserInfoAsync(cancellationToken)
                .ConfigureAwait(false);
            return info.Id.Length > 0;
        }
        catch (PhoenixReadApiException)
        {
            Logout();
            return false;
        }
    }

    internal async Task<QrCodeLoginSession> CreateQrCodeLoginAsync(
        CancellationToken cancellationToken
    )
    {
        WebPassportResponse<WebQrCodeData> response = await _appTransport
            .GetWebPassportJsonAsync<WebQrCodeData>(
                "/passport/web/get_qrcode/",
                [new("next", "https://hongguoduanju.com/")],
                cancellationToken
            )
            .ConfigureAwait(false);
        WebQrCodeData data = response.Data ?? new();
        string token = data.Token.Trim();
        string rawQr = data.QrCode.Trim();
        if (token.Length == 0 || rawQr.Length == 0)
        {
            throw new PhoenixReadApiProviderException(
                "获取网页登录二维码失败，返回数据不完整。",
                "/passport/web/get_qrcode/",
                "EMPTY_QR",
                isTransient: true
            );
        }

        string base64Only = rawQr.Contains("base64,", StringComparison.OrdinalIgnoreCase)
            ? rawQr.Split("base64,")[^1]
            : rawQr;

        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(base64Only);
        }
        catch (FormatException)
        {
            imageBytes = [];
        }

        long rawExpiry = data.ExpireTime;
        long nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int expiresInSeconds = rawExpiry > 1_000_000_000
            ? (int)Math.Max(0, Math.Min(600, rawExpiry - nowSeconds))
            : (int)Math.Max(0, Math.Min(600, rawExpiry > 0 ? rawExpiry : 180));

        return new QrCodeLoginSession
        {
            Token = token,
            QrCodeBase64 = base64Only,
            QrCodeImageBytes = imageBytes,
            ExpiresInSeconds = expiresInSeconds,
        };
    }

    internal async Task<QrCodeCheckResult> CheckQrCodeStatusAsync(
        string token,
        CancellationToken cancellationToken
    )
    {
        token = (token ?? string.Empty).Trim();
        if (token.Length == 0)
        {
            throw new ArgumentException("扫码凭证 Token 不能为空。", nameof(token));
        }

        WebPassportResponse<WebQrStatusData> response = await _appTransport
            .GetWebPassportJsonAsync<WebQrStatusData>(
                "/passport/web/check_qrconnect/",
                [
                    new("next", "https://hongguoduanju.com/"),
                    new("token", token),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        WebQrStatusData data = response.Data ?? new();
        string status = (data.Status ?? string.Empty).Trim();

        if (status is "5" or "expired")
        {
            return new QrCodeCheckResult { Status = QrCodeLoginStatus.Expired };
        }

        if (status is "2" or "scanned")
        {
            return new QrCodeCheckResult { Status = QrCodeLoginStatus.Scanned };
        }

        if (status is "3" or "confirmed")
        {
            if (!string.IsNullOrWhiteSpace(data.RedirectUrl))
            {
                await _appTransport
                    .FollowRedirectAsync(data.RedirectUrl, cancellationToken)
                    .ConfigureAwait(false);
            }

            WebPassportResponse<WebPassportAccountInfoData> profileResponse = await _appTransport
                .GetWebPassportJsonAsync<WebPassportAccountInfoData>(
                    "/passport/account/info/v2/",
                    [],
                    cancellationToken
                )
                .ConfigureAwait(false);
            WebPassportAccountInfoData profile = profileResponse.Data ?? new();
            string userId = !string.IsNullOrWhiteSpace(profile.UserIdStr)
                ? profile.UserIdStr
                : profile.UserId > 0
                    ? profile.UserId.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;

            _user = new AuthenticatedUser
            {
                Id = userId,
                Name = !string.IsNullOrWhiteSpace(profile.Name)
                    ? profile.Name
                    : !string.IsNullOrWhiteSpace(profile.ScreenName)
                        ? profile.ScreenName
                        : "红果用户",
                AvatarUrl = profile.AvatarUrl,
                Description = string.Empty,
                MaskedPhone = string.Empty,
            };
            _appTransport.SaveAuthenticatedUser(_user);

            return new QrCodeCheckResult
            {
                Status = QrCodeLoginStatus.Confirmed,
                User = _user,
            };
        }

        return new QrCodeCheckResult { Status = QrCodeLoginStatus.WaitingForScan };
    }

    private static string NormalizePhone(string value)
    {
        value = new string([.. (value ?? string.Empty).Where(static c => !char.IsWhiteSpace(c))]);
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
        for (int index = 0; index < bytes.Length; index++)
        {
            bytes[index] ^= 5;
        }
        return Convert.ToHexStringLower(bytes);
    }
}
