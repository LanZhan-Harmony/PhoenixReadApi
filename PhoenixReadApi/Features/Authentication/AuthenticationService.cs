using System.Globalization;
using System.Text;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Authentication;
using PhoenixReadApi.Internal.Contracts.App.User;
using PhoenixReadApi.Internal.Contracts.Web.Authentication;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi.Internal.Features.Authentication;

/// <summary>负责短信登录和会话状态。</summary>
internal sealed class AuthenticationService : PhoenixFeatureService
{
    private static readonly IReadOnlyList<LoginRegion> s_defaultRegions =
    [
        new() { Name = "中国大陆", Code = "86", Iso = "CN", MinLength = 11, MaxLength = 11 },
        new() { Name = "中国香港", Code = "852", Iso = "HK", MinLength = 8, MaxLength = 8 },
        new() { Name = "中国澳门", Code = "853", Iso = "MO", MinLength = 8, MaxLength = 8 },
        new() { Name = "中国台湾", Code = "886", Iso = "TW", MinLength = 9, MaxLength = 10 },
        new() { Name = "美国", Code = "1", Iso = "US", MinLength = 10, MaxLength = 10 },
        new() { Name = "加拿大", Code = "1", Iso = "CA", MinLength = 10, MaxLength = 10 },
        new() { Name = "新加坡", Code = "65", Iso = "SG", MinLength = 8, MaxLength = 12 },
        new() { Name = "马来西亚", Code = "60", Iso = "MY", MinLength = 9, MaxLength = 11 },
        new() { Name = "日本", Code = "81", Iso = "JP", MinLength = 10, MaxLength = 11 },
        new() { Name = "韩国", Code = "82", Iso = "KR", MinLength = 9, MaxLength = 11 },
        new() { Name = "英国", Code = "44", Iso = "GB", MinLength = 10, MaxLength = 11 },
        new() { Name = "澳大利亚", Code = "61", Iso = "AU", MinLength = 9, MaxLength = 10 },
    ];

    private AuthenticatedUser _user = new();

    internal AuthenticationService(PhoenixReadClientRuntime runtime)
        : base(runtime)
    {
        _user = _appTransport.RestoredUser;
    }

    internal IReadOnlyList<LoginRegion> GetLoginRegions() => s_defaultRegions;

    internal Task<IReadOnlyList<LoginRegion>> GetLoginRegionsAsync(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(s_defaultRegions);
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

    internal async Task<AuthenticatedUser> GetCurrentUserInfoAsync(
        CancellationToken cancellationToken
    )
    {
        AppResponse<AppCurrentUserInfoData> response = await _appTransport
            .GetAsync<AppCurrentUserInfoData>("/reading/user/info/v1/", [], cancellationToken)
            .ConfigureAwait(false);
        AppCurrentUserInfoData data = response.Data ?? new();
        string userId = !string.IsNullOrEmpty(data.UserId) ? data.UserId : data.BizUserId;
        AuthenticatedUser info = new()
        {
            Id = userId,
            Name = data.UserName,
            AvatarUrl = data.AvatarUrl,
            Description = data.Description,
            MaskedPhone = _user.MaskedPhone,
            Gender = data.Gender,
            IsVip = data.VipInfo.IsVip == "1",
        };

        if (userId.Length > 0)
        {
            _user = info;
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
            AuthenticatedUser info = await GetCurrentUserInfoAsync(cancellationToken)
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
