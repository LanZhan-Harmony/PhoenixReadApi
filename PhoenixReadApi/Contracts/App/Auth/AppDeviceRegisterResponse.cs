using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Auth;

/// <summary>设备注册响应。</summary>
internal sealed class AppDeviceRegisterResponse
{
    [JsonPropertyName("device_id_str")]
    public string DeviceIdStr { get; init; } = string.Empty;

    [JsonPropertyName("install_id_str")]
    public string InstallIdStr { get; init; } = string.Empty;

    [JsonPropertyName("device_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string DeviceId { get; init; } = string.Empty;

    [JsonPropertyName("install_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string InstallId { get; init; } = string.Empty;

    [JsonPropertyName("klink_egdi")]
    public string KlinkEgdi { get; init; } = string.Empty;

    [JsonPropertyName("device_token")]
    public string DeviceToken { get; init; } = string.Empty;

    [JsonPropertyName("server_time")]
    public long ServerTime { get; init; }

    [JsonPropertyName("new_user")]
    public int NewUser { get; init; }

    [JsonIgnore]
    public string EffectiveDeviceId =>
        !string.IsNullOrWhiteSpace(DeviceIdStr) ? DeviceIdStr : DeviceId;

    [JsonIgnore]
    public string EffectiveInstallId =>
        !string.IsNullOrWhiteSpace(InstallIdStr) ? InstallIdStr : InstallId;
}
