using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App.Auth;
using PhoenixReadApi.Internal.Infrastructure.Crypto;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>负责红果客户端的 Cookie、认证头、会话快照与诊断文件持久化。</summary>
internal sealed class PhoenixReadSessionStore
{
    private static readonly string[] AuthHeaderNames =
    [
        "x-tt-token",
        "x-tt-token-sign",
        "x-tt-token-supplement",
        "x-tt-passport-mfa-token",
        "x-tt-passport-csrf-token",
        "d-ticket",
    ];

    private static readonly string[] CleanupFileExtensions =
    [
        "",
        ".response.json",
        ".tmp",
    ];

    private readonly ConcurrentDictionary<string, string> _cookies = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly ConcurrentDictionary<string, string> _authHeaders = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly string? _sessionStateFilePath;
    private readonly Lock _sessionFileLock = new();
    private volatile string? _cachedCookieHeader;

    internal string Cdid { get; set; } = string.Empty;
    internal string TtReq { get; set; } = string.Empty;
    internal string PassportCsrfToken { get; set; } = string.Empty;
    internal string UserId { get; set; } = string.Empty;
    internal string UserName { get; set; } = string.Empty;
    internal string UserAvatarUrl { get; set; } = string.Empty;
    internal string UserDescription { get; set; } = string.Empty;
    internal string UserMaskedPhone { get; set; } = string.Empty;
    internal string DeviceId { get; set; } = string.Empty;
    internal string InstallId { get; set; } = string.Empty;
    internal string KlinkEgdi { get; set; } = string.Empty;
    internal bool IsRegistered { get; set; }

    internal PhoenixReadSessionStore(PhoenixReadApiOptions options)
    {
        _sessionStateFilePath =
            options.AuthenticationStateFilePath ?? options.AnonymousStateFilePath;
        DeviceId = options.DeviceId!;
        InstallId = options.InstallId!;
        LoadSessionSnapshot();
        if (string.IsNullOrWhiteSpace(Cdid))
        {
            Cdid = Guid.NewGuid().ToString();
        }
        if (string.IsNullOrWhiteSpace(TtReq))
        {
            Span<byte> value = stackalloc byte[16];
            RandomNumberGenerator.Fill(value);
            TtReq = "1$" + Convert.ToHexStringLower(value);
        }
    }

    internal bool HasSession =>
        _authHeaders.ContainsKey("x-tt-token")
        && (_cookies.ContainsKey("sessionid") || _cookies.ContainsKey("sid_tt"));

    internal AuthenticatedUser RestoredUser =>
        new()
        {
            Id = UserId,
            Name = UserName,
            AvatarUrl = UserAvatarUrl,
            Description = UserDescription,
            MaskedPhone = UserMaskedPhone,
        };

    internal void SaveAuthenticatedUser(AuthenticatedUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        UserId = user.Id;
        UserName = user.Name;
        UserAvatarUrl = user.AvatarUrl;
        UserDescription = user.Description;
        UserMaskedPhone = user.MaskedPhone;
        SaveSessionSnapshot();
    }

    internal void SaveAuthenticationMetadata(
        string verificationTicket,
        string maskedPhone,
        int retryAfterSeconds
    ) => SaveSessionSnapshot(verificationTicket, maskedPhone, retryAfterSeconds);

    internal void ClearSession()
    {
        _cookies.Clear();
        _authHeaders.Clear();
        _cachedCookieHeader = null;
        PassportCsrfToken = string.Empty;
        UserId = string.Empty;
        UserName = string.Empty;
        UserAvatarUrl = string.Empty;
        UserDescription = string.Empty;
        UserMaskedPhone = string.Empty;
        DeleteSessionSnapshot();
    }

    internal void ApplySession(HttpRequestMessage request)
    {
        if (!_cookies.IsEmpty)
        {
            string? cookieHeader = _cachedCookieHeader;
            if (cookieHeader is null)
            {
                cookieHeader = BuildCookieHeader();
                _cachedCookieHeader = cookieHeader;
            }

            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

        foreach (KeyValuePair<string, string> header in _authHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private string BuildCookieHeader()
    {
        System.Text.StringBuilder sb = new();
        bool first = true;
        foreach (KeyValuePair<string, string> pair in _cookies)
        {
            if (!first)
            {
                sb.Append("; ");
            }
            first = false;
            sb.Append(pair.Key);
            sb.Append('=');
            sb.Append(pair.Value);
        }
        return sb.ToString();
    }

    internal void CaptureSession(HttpResponseMessage response)
    {
        bool changed = false;
        if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values))
        {
            foreach (string value in values)
            {
                int semiIndex = value.IndexOf(';');
                ReadOnlySpan<char> cookiePart = semiIndex >= 0 ? value.AsSpan(0, semiIndex).Trim() : value.AsSpan().Trim();
                int eqIndex = cookiePart.IndexOf('=');
                if (eqIndex > 0)
                {
                    string key = cookiePart[..eqIndex].ToString();
                    string val = cookiePart[(eqIndex + 1)..].ToString();
                    if (val.Length == 0)
                    {
                        if (_cookies.TryRemove(key, out _))
                        {
                            changed = true;
                        }
                    }
                    else
                    {
                        if (!_cookies.TryGetValue(key, out string? existing) || existing != val)
                        {
                            _cookies[key] = val;
                            changed = true;
                        }
                    }
                }
            }
        }

        foreach (string name in AuthHeaderNames)
        {
            if (response.Headers.TryGetValues(name, out IEnumerable<string>? headerValues))
            {
                string? value = headerValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (!_authHeaders.TryGetValue(name, out string? existing) || existing != value)
                    {
                        _authHeaders[name] = value;
                        changed = true;
                    }
                }
            }
        }

        if (changed)
        {
            _cachedCookieHeader = null;
            SaveSessionSnapshot();
        }
    }

    internal void EnsureAuthenticationSession()
    {
        if (string.IsNullOrWhiteSpace(Cdid))
        {
            Cdid = Guid.NewGuid().ToString();
        }
        if (string.IsNullOrWhiteSpace(PassportCsrfToken))
        {
            Span<byte> token = stackalloc byte[16];
            RandomNumberGenerator.Fill(token);
            PassportCsrfToken = Convert.ToHexStringLower(token);
        }

        _cookies.TryAdd("install_id", InstallId);
        _cookies.TryAdd("store-region", "cn-ah");
        _cookies.TryAdd("store-region-src", "did");
        _cookies.TryAdd("ttreq", TtReq);
        _cookies.TryAdd("passport_csrf_token", PassportCsrfToken);
        _cookies.TryAdd("passport_csrf_token_default", PassportCsrfToken);
        _authHeaders["x-tt-passport-csrf-token"] = PassportCsrfToken;
        SaveSessionSnapshot();
    }

    internal void UpdateDeviceIdentity(
        string deviceId,
        string installId,
        string cdid,
        string klinkEgdi,
        bool registered
    )
    {
        DeviceId = deviceId;
        InstallId = installId;
        Cdid = cdid;
        if (!string.IsNullOrWhiteSpace(klinkEgdi))
        {
            KlinkEgdi = klinkEgdi;
        }
        IsRegistered = registered;
        SaveSessionSnapshot();
    }

    private void LoadSessionSnapshot()
    {
        if (_sessionStateFilePath is null || !File.Exists(_sessionStateFilePath))
        {
            return;
        }

        try
        {
            AppSessionSnapshot? snapshot = JsonAotSerializer.Deserialize<AppSessionSnapshot>(
                File.ReadAllText(_sessionStateFilePath)
            );
            if (snapshot is null)
            {
                return;
            }
            foreach ((string key, string value) in snapshot.Cookies)
            {
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    _cookies[key] = value;
                }
            }
            foreach ((string key, string value) in snapshot.Headers)
            {
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    _authHeaders[key] = value;
                }
            }
            Cdid = snapshot.Cdid;
            if (DeviceIdGenerator.IsValid(snapshot.DeviceId))
            {
                DeviceId = snapshot.DeviceId;
            }
            if (DeviceIdGenerator.IsValid(snapshot.InstallId))
            {
                InstallId = snapshot.InstallId;
            }
            KlinkEgdi = string.IsNullOrWhiteSpace(snapshot.KlinkEgdi)
                ? KlinkEgdi
                : snapshot.KlinkEgdi;
            IsRegistered =
                snapshot.Registered
                && DeviceIdGenerator.IsValid(DeviceId)
                && DeviceIdGenerator.IsValid(InstallId);
            PassportCsrfToken = snapshot.PassportCsrfToken;
            TtReq = snapshot.TtReq;
            UserId = snapshot.UserId;
            UserName = snapshot.UserName;
            UserAvatarUrl = snapshot.UserAvatarUrl;
            UserDescription = snapshot.UserDescription;
            UserMaskedPhone = snapshot.UserMaskedPhone;
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            // A stale or inaccessible optional snapshot must not prevent anonymous use.
        }
    }

    internal void SaveSessionSnapshot(
        string? verificationTicket = null,
        string? maskedPhone = null,
        int? retryAfterSeconds = null
    )
    {
        if (_sessionStateFilePath is null)
        {
            return;
        }

        lock (_sessionFileLock)
        {
            try
            {
                string? directory = Path.GetDirectoryName(_sessionStateFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AppSessionSnapshot old = File.Exists(_sessionStateFilePath)
                    ? JsonAotSerializer.Deserialize<AppSessionSnapshot>(
                        File.ReadAllText(_sessionStateFilePath)
                    ) ?? new()
                    : new();
                AppSessionSnapshot snapshot = new()
                {
                    Cookies = new Dictionary<string, string>(
                        _cookies,
                        StringComparer.OrdinalIgnoreCase
                    ),
                    Headers = new Dictionary<string, string>(
                        _authHeaders,
                        StringComparer.OrdinalIgnoreCase
                    ),
                    VerificationTicket = verificationTicket ?? old.VerificationTicket,
                    MaskedPhone = maskedPhone ?? old.MaskedPhone,
                    RetryAfterSeconds = retryAfterSeconds ?? old.RetryAfterSeconds,
                    Cdid = Cdid,
                    KlinkEgdi = KlinkEgdi,
                    Registered = IsRegistered,
                    PassportCsrfToken = PassportCsrfToken,
                    TtReq = TtReq,
                    DeviceId = DeviceId,
                    InstallId = InstallId,
                    UserId = UserId,
                    UserName = UserName,
                    UserAvatarUrl = UserAvatarUrl,
                    UserDescription = UserDescription,
                    UserMaskedPhone = UserMaskedPhone,
                    UpdatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                };
                string temporary = _sessionStateFilePath + ".tmp";
                File.WriteAllText(temporary, JsonAotSerializer.Serialize(snapshot));
                File.Move(temporary, _sessionStateFilePath, overwrite: true);
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                // Session persistence is best effort; network calls remain usable when the path is unavailable.
            }
        }
    }

    internal void DeleteSessionSnapshot()
    {
        if (_sessionStateFilePath is null)
        {
            return;
        }

        lock (_sessionFileLock)
        {
            foreach (string ext in CleanupFileExtensions)
            {
                try
                {
                    string path = _sessionStateFilePath + ext;
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception exception)
                    when (exception is IOException or UnauthorizedAccessException)
                {
                    // Best effort cleanup; the in-memory session is already cleared.
                }
            }
        }
    }

    internal void SaveAuthenticationResponseDiagnostic(
        string path,
        HttpResponseMessage response,
        byte[] body
    )
    {
        if (_sessionStateFilePath is null || !path.Contains("/passport/", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            string diagnosticPath = _sessionStateFilePath + ".response.json";
            string headers = string.Join(
                "\n",
                response.Headers.SelectMany(static pair =>
                    pair.Value.Select(value => $"{pair.Key}: {value}")
                )
            );
            File.WriteAllText(
                diagnosticPath,
                $"Path: {path}\nStatus: {(int)response.StatusCode}\nHeaders:\n{headers}\nBody:\n{System.Text.Encoding.UTF8.GetString(body)}\n"
            );
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }
}
