using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using PhoenixReadApi.Internal.Contracts.App.Authentication;
using PhoenixReadApi.Internal.Infrastructure.Crypto;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>负责管理红果客户端的设备身份、设备注册以及被拒时的自动换号。</summary>
internal sealed class PhoenixReadDeviceManager
{
    private readonly PhoenixReadHttpTransport _http;
    private readonly string _userAgent;
    private readonly PhoenixReadSessionStore _sessionStore;
    private readonly SemaphoreSlim _deviceLock = new(1, 1);
    private DateTimeOffset _registerPausedUntil = DateTimeOffset.MinValue;
    private volatile string? _cachedAppQuery;
    private volatile bool _isActivated;

    internal string DeviceId
    {
        get;
        set
        {
            field = value;
            _cachedAppQuery = null;
        }
    }
    internal string InstallId
    {
        get;
        set
        {
            field = value;
            _cachedAppQuery = null;
        }
    }
    internal string Cdid
    {
        get;
        set
        {
            field = value;
            _cachedAppQuery = null;
        }
    }
    internal string KlinkEgdi
    {
        get;
        set
        {
            field = value;
            _cachedAppQuery = null;
        }
    }
    internal string OpenUdid
    {
        get => _sessionStore.OpenUdid;
        set => _sessionStore.OpenUdid = value;
    }
    internal bool IsRegistered { get; set; }

    internal PhoenixReadDeviceManager(
        PhoenixReadHttpTransport http,
        string userAgent,
        PhoenixReadSessionStore sessionStore
    )
    {
        _http = http;
        _userAgent = userAgent;
        _sessionStore = sessionStore;
        DeviceId = sessionStore.DeviceId;
        InstallId = sessionStore.InstallId;
        Cdid = sessionStore.Cdid;
        KlinkEgdi = sessionStore.KlinkEgdi;
        IsRegistered = sessionStore.IsRegistered;
    }

    internal static readonly string StaticAppQueryPrefix = PhoenixReadDeviceProfile.StaticAppQueryPrefix;

    internal void AppendAppQuery(StringBuilder sb)
    {
        sb.Append(GetCachedAppQuery());
    }

    private string GetCachedAppQuery()
    {
        string? cached = _cachedAppQuery;
        if (cached is not null)
        {
            return cached;
        }

        StringBuilder sb = new(512);
        sb.Append(StaticAppQueryPrefix);
        sb.Append("&device_id=")
            .Append(DeviceIdGenerator.IsValid(DeviceId) ? DeviceId : "1589262967141403");
        sb.Append("&iid=")
            .Append(DeviceIdGenerator.IsValid(InstallId) ? InstallId : "2380912043756905");
        string cdid = string.IsNullOrWhiteSpace(Cdid)
            ? "c179f4ce-dbbe-4c3e-ae27-8d462e63782b"
            : Cdid;
        sb.Append("&cdid=").Append(Uri.EscapeDataString(cdid));
        string klink = string.IsNullOrWhiteSpace(KlinkEgdi)
            ? "AAJgUhu9EVIH0a1aMHvCy177wq-bhqHT3XS5vNNgeIgolcgOrGQOGZwL"
            : KlinkEgdi;
        sb.Append("&klink_egdi=").Append(Uri.EscapeDataString(klink));

        cached = sb.ToString();
        _cachedAppQuery = cached;
        return cached;
    }

    internal async Task EnsureRegisteredAsync(bool forceNew, CancellationToken cancellationToken)
    {
        if (
            !forceNew
            && IsRegistered
            && DeviceIdGenerator.IsValid(DeviceId)
            && DeviceIdGenerator.IsValid(InstallId)
        )
        {
            if (!_isActivated)
            {
                _isActivated = true;
                _ = ActivateDeviceAsync(DeviceId, InstallId, cancellationToken);
            }
            return;
        }

        await _deviceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (
                !forceNew
                && IsRegistered
                && DeviceIdGenerator.IsValid(DeviceId)
                && DeviceIdGenerator.IsValid(InstallId)
            )
            {
                if (!_isActivated)
                {
                    _isActivated = true;
                    _ = ActivateDeviceAsync(DeviceId, InstallId, cancellationToken);
                }
                return;
            }

            if (!forceNew && DateTimeOffset.UtcNow < _registerPausedUntil)
            {
                return;
            }

            await RegisterDeviceCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _deviceLock.Release();
        }
    }

    internal async Task ResetDeviceAsync(CancellationToken cancellationToken)
    {
        await _deviceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _registerPausedUntil = DateTimeOffset.MinValue;
            _cachedAppQuery = null;
            _sessionStore.ResetAnonymousDevice();
            DeviceId = string.Empty;
            InstallId = string.Empty;
            IsRegistered = false;
            await RegisterDeviceCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _deviceLock.Release();
        }
    }

    private async Task RegisterDeviceCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string newCdid = Guid.NewGuid().ToString();
            string clientUdid = Guid.NewGuid().ToString();
            string openUdid = Convert
                .ToHexString(RandomNumberGenerator.GetBytes(8))
                .ToLowerInvariant();
            string reqId = Guid.NewGuid().ToString();
            string oaid = Guid.NewGuid().ToString();

            AppDeviceRegisterPayload payload = new()
            {
                MagicTag = "ss_app_log",
                GenTime = nowMs,
                Header = new AppDeviceRegisterHeader
                {
                    Cdid = newCdid,
                    OpenUdid = openUdid,
                    ClientUdid = clientUdid,
                    ReqId = reqId,
                    Oaid = new AppDeviceRegisterOaid
                    {
                        ReqId = oaid,
                        Id = oaid,
                        Time = (nowMs - 200).ToString(CultureInfo.InvariantCulture),
                    },
                },
            };

            byte[] jsonBytes = JsonAotSerializer.SerializeToUtf8Bytes(payload);
            byte[] encryptedBody = TtEncrypt.GzipAndEncrypt(jsonBytes);
            string bodyStub = Convert.ToHexString(MD5.HashData(encryptedBody));

            KeyValuePair<string, string>[] registerQuery =
            [
                new("req_id", reqId),
                new("tt_data", "a"),
                new("aid", PhoenixReadDeviceProfile.AidString),
                new("app_name", PhoenixReadDeviceProfile.AppName),
                new("version_code", PhoenixReadDeviceProfile.VersionCode.ToString(CultureInfo.InvariantCulture)),
                new("version_name", PhoenixReadDeviceProfile.VersionName),
                new("manifest_version_code", PhoenixReadDeviceProfile.VersionCode.ToString(CultureInfo.InvariantCulture)),
                new("update_version_code", PhoenixReadDeviceProfile.VersionCode.ToString(CultureInfo.InvariantCulture)),
                new("channel", PhoenixReadDeviceProfile.Channel),
                new("device_platform", PhoenixReadDeviceProfile.DevicePlatform),
                new("os", "android"),
                new("ssmix", "a"),
                new("device_type", PhoenixReadDeviceProfile.DeviceModel),
                new("device_brand", PhoenixReadDeviceProfile.DeviceBrand),
                new("language", PhoenixReadDeviceProfile.Language),
                new("os_api", PhoenixReadDeviceProfile.OsApi.ToString(CultureInfo.InvariantCulture)),
                new("os_version", PhoenixReadDeviceProfile.OsVersion),
                new("resolution", PhoenixReadDeviceProfile.Resolution),
                new("dpi", PhoenixReadDeviceProfile.Dpi.ToString(CultureInfo.InvariantCulture)),
                new("ac", PhoenixReadDeviceProfile.Access),
                new("_rticket", nowMs.ToString(CultureInfo.InvariantCulture)),
            ];

            Uri registerUri = HttpUriHelper.CreateRawUri(
                new Uri("https://log5-applog-lq.fqnovel.com/"),
                "service/2/device_register/",
                registerQuery
            );
            using HttpRequestMessage request = new(HttpMethod.Post, registerUri);
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("log-encode-type", "gzip");
            request.Headers.TryAddWithoutValidation("sdk-version", "2");
            request.Headers.TryAddWithoutValidation("passport-sdk-version", "5051452");
            request.Headers.TryAddWithoutValidation("x-vc-bdturing-sdk-version", "4.0.3.cn");
            request.Headers.TryAddWithoutValidation("x-ss-dp", "8662");
            request.Headers.TryAddWithoutValidation("X-SS-STUB", bodyStub);
            request.Content = new ByteArrayContent(encryptedBody);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(
                "application/octet-stream"
            )
            {
                Parameters = { new NameValueHeaderValue("tt-data", "a") },
            };

            using HttpResponseMessage response = await _http
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _registerPausedUntil = DateTimeOffset.UtcNow.AddMinutes(5);
                return;
            }

            _sessionStore.CaptureSession(response);

            byte[] bytes = await _http
                .ReadBodyAsync(response, cancellationToken)
                .ConfigureAwait(false);
            if (bytes.Length == 0)
            {
                _registerPausedUntil = DateTimeOffset.UtcNow.AddMinutes(5);
                return;
            }

            AppDeviceRegisterResponse? reg =
                JsonAotSerializer.Deserialize<AppDeviceRegisterResponse>(bytes);
            if (reg is not null)
            {
                string devId = reg.EffectiveDeviceId;
                string instId = reg.EffectiveInstallId;
                if (DeviceIdGenerator.IsValid(devId) && DeviceIdGenerator.IsValid(instId))
                {
                    DeviceId = devId;
                    InstallId = instId;
                    Cdid = newCdid;
                    if (!string.IsNullOrWhiteSpace(reg.KlinkEgdi))
                    {
                        KlinkEgdi = reg.KlinkEgdi;
                    }
                    IsRegistered = true;
                    _registerPausedUntil = DateTimeOffset.MinValue;
                    _isActivated = true;
                    _sessionStore.UpdateDeviceIdentity(
                        DeviceId,
                        InstallId,
                        Cdid,
                        KlinkEgdi,
                        IsRegistered,
                        openUdid
                    );

                    // 设备注册完成后立即调用 app_alert_check 激活心跳，解除网关 (tt_agw) 对未激活匿名设备的静默拦截
                    await ActivateDeviceAsync(DeviceId, InstallId, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _registerPausedUntil = DateTimeOffset.UtcNow.AddMinutes(5);
        }
    }

    /// <summary>
    /// 调用 /service/2/app_alert_check/ 激活新注册或已恢复的设备，使其在 tt_agw 网关生效。
    /// </summary>
    private async Task ActivateDeviceAsync(
        string deviceId,
        string installId,
        CancellationToken cancellationToken
    )
    {
        if (!DeviceIdGenerator.IsValid(deviceId) || !DeviceIdGenerator.IsValid(installId))
        {
            return;
        }

        try
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            KeyValuePair<string, string>[] query =
            [
                new("aid", PhoenixReadDeviceProfile.AidString),
                new("app_name", PhoenixReadDeviceProfile.AppName),
                new("channel", PhoenixReadDeviceProfile.Channel),
                new("device_id", deviceId),
                new("iid", installId),
                new("device_platform", PhoenixReadDeviceProfile.DevicePlatform),
                new("os", "android"),
                new("version_code", PhoenixReadDeviceProfile.VersionCode.ToString(CultureInfo.InvariantCulture)),
                new("version_name", PhoenixReadDeviceProfile.VersionName),
                new("_rticket", nowMs.ToString(CultureInfo.InvariantCulture)),
            ];

            Uri activateUri = HttpUriHelper.CreateRawUri(
                new Uri("https://log5-applog-lf.fqnovel.com/"),
                "service/2/app_alert_check/",
                query
            );

            using HttpRequestMessage request = new(HttpMethod.Get, activateUri);
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);

            using HttpResponseMessage response = await _http
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            _ = await _http.ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 心跳激活失败不阻断流程
        }
    }
}
