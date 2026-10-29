using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using PhoenixReadApi.Internal.Contracts.App.Auth;
using PhoenixReadApi.Internal.Infrastructure.Crypto;
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

    internal string DeviceId { get; set; } = string.Empty;
    internal string InstallId { get; set; } = string.Empty;
    internal string Cdid { get; set; } = string.Empty;
    internal string KlinkEgdi { get; set; } = string.Empty;
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

    internal KeyValuePair<string, string>[] BuildAppQuery() =>
        [
            new("aid", "8662"),
            new("app_name", "novelread"),
            new("version_code", "69132"),
            new("version_name", "6.9.1.32"),
            new("manifest_version_code", "69132"),
            new("update_version_code", "69132"),
            new("channel", "update_64"),
            new("device_platform", "android"),
            new("os", "android"),
            new("ssmix", "a"),
            new("device_type", "WKL-AN20"),
            new("device_brand", "HONOR"),
            new("language", "zh"),
            new("os_api", "37"),
            new("os_version", "17"),
            new("resolution", "1320*2868"),
            new("dpi", "360"),
            new("ac", "wifi"),
            new("device_id", DeviceId),
            new("iid", InstallId),
        ];

    internal async Task EnsureRegisteredAsync(bool forceNew, CancellationToken cancellationToken)
    {
        if (
            !forceNew
            && IsRegistered
            && DeviceIdGenerator.IsValid(DeviceId)
            && DeviceIdGenerator.IsValid(InstallId)
        )
        {
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
                return;
            }

            if (DateTimeOffset.UtcNow < _registerPausedUntil)
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

            AppDeviceRegisterPayload payload = new()
            {
                MagicTag = "ss_app_log",
                GenTime = nowMs,
                Header = new AppDeviceRegisterHeader
                {
                    OpenUdid = openUdid,
                    ClientUdid = clientUdid,
                    Cdid = newCdid,
                },
            };

            byte[] body = JsonAotSerializer.SerializeToUtf8Bytes(payload);
            KeyValuePair<string, string>[] registerQuery =
            [
                new("aid", "8662"),
                new("app_name", "novelread"),
                new("version_code", "69132"),
                new("version_name", "6.9.1.32"),
                new("manifest_version_code", "69132"),
                new("update_version_code", "69132"),
                new("channel", "update_64"),
                new("device_platform", "android"),
                new("os", "android"),
                new("ssmix", "a"),
                new("device_type", "WKL-AN20"),
                new("device_brand", "HONOR"),
                new("language", "zh"),
                new("os_api", "37"),
                new("os_version", "17"),
                new("resolution", "1320*2868"),
                new("dpi", "360"),
                new("ac", "wifi"),
                new("req_id", Guid.NewGuid().ToString()),
                new("_rticket", nowMs.ToString(CultureInfo.InvariantCulture)),
            ];

            Uri registerUri = HttpUriHelper.CreateRawUri(
                new Uri("https://log.snssdk.com/"),
                "service/2/device_register/",
                registerQuery
            );
            using HttpRequestMessage request = new(HttpMethod.Post, registerUri);
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Content = new ByteArrayContent(body);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
            {
                CharSet = "utf-8",
            };

            using HttpResponseMessage response = await _http
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _registerPausedUntil = DateTimeOffset.UtcNow.AddMinutes(10);
                return;
            }

            byte[] bytes = await _http
                .ReadBodyAsync(response, cancellationToken)
                .ConfigureAwait(false);
            if (bytes.Length == 0)
            {
                _registerPausedUntil = DateTimeOffset.UtcNow.AddMinutes(10);
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
                    _sessionStore.UpdateDeviceIdentity(
                        DeviceId,
                        InstallId,
                        Cdid,
                        KlinkEgdi,
                        IsRegistered
                    );
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _registerPausedUntil = DateTimeOffset.UtcNow.AddMinutes(10);
        }
    }
}
