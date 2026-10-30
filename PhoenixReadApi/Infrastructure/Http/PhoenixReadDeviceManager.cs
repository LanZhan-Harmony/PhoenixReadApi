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

    internal KeyValuePair<string, string>[] BuildAppQuery()
    {
        List<KeyValuePair<string, string>> query =
        [
            new("aid", "8662"),
            new("app_name", "novelread"),
            new("version_code", "69132"),
            new("version_name", "6.9.1.32"),
            new("manifest_version_code", "73932"),
            new("update_version_code", "73932"),
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
            new("compliance_status", "0"),
            new("need_personal_recommend", "1"),
            new("player_so_load", "1"),
            new("is_android_pad_screen", "0"),
            new("rom_version", "V417IR release-keys"),
            new("dragon_device_type", "phone"),
            new("host_abi", "arm64-v8a"),
            new("device_id", string.IsNullOrWhiteSpace(DeviceId) ? "1589262967141403" : DeviceId),
            new("iid", string.IsNullOrWhiteSpace(InstallId) ? "2380912043756905" : InstallId),
            new("pv_player", "73932"),
        ];

        string cdid = string.IsNullOrWhiteSpace(Cdid)
            ? "c179f4ce-dbbe-4c3e-ae27-8d462e63782b"
            : Cdid;
        query.Add(new("cdid", cdid));

        string klink = string.IsNullOrWhiteSpace(KlinkEgdi)
            ? "AAJgUhu9EVIH0a1aMHvCy177wq-bhqHT3XS5vNNgeIgolcgOrGQOGZwL"
            : KlinkEgdi;
        query.Add(new("klink_egdi", klink));

        return [.. query];
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
