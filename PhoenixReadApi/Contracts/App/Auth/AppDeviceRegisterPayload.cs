using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Auth;

/// <summary>设备注册请求载荷。</summary>
internal sealed class AppDeviceRegisterPayload
{
    [JsonPropertyName("magic_tag")]
    public string MagicTag { get; init; } = "ss_app_log";

    [JsonPropertyName("header")]
    public AppDeviceRegisterHeader Header { get; init; } = new();

    [JsonPropertyName("_gen_time")]
    public long GenTime { get; init; }
}

/// <summary>设备注册请求头信息。</summary>
internal sealed class AppDeviceRegisterHeader
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; init; } = "红果免费短剧";

    [JsonPropertyName("aid")]
    public int Aid { get; init; } = 8662;

    [JsonPropertyName("app_version")]
    public string AppVersion { get; init; } = "6.9.1.32";

    [JsonPropertyName("version_code")]
    public int VersionCode { get; init; } = 69132;

    [JsonPropertyName("manifest_version_code")]
    public int ManifestVersionCode { get; init; } = 69132;

    [JsonPropertyName("update_version_code")]
    public int UpdateVersionCode { get; init; } = 69132;

    [JsonPropertyName("channel")]
    public string Channel { get; init; } = "update_64";

    [JsonPropertyName("package")]
    public string Package { get; init; } = "com.phoenix.read";

    [JsonPropertyName("app_language")]
    public string AppLanguage { get; init; } = "zh";

    [JsonPropertyName("os")]
    public string Os { get; init; } = "Android";

    [JsonPropertyName("os_version")]
    public string OsVersion { get; init; } = "17";

    [JsonPropertyName("os_api")]
    public int OsApi { get; init; } = 37;

    [JsonPropertyName("device_model")]
    public string DeviceModel { get; init; } = "WKL-AN20";

    [JsonPropertyName("device_brand")]
    public string DeviceBrand { get; init; } = "HONOR";

    [JsonPropertyName("device_manufacturer")]
    public string DeviceManufacturer { get; init; } = "HONOR";

    [JsonPropertyName("cpu_abi")]
    public string CpuAbi { get; init; } = "arm64-v8a";

    [JsonPropertyName("density_dpi")]
    public int DensityDpi { get; init; } = 360;

    [JsonPropertyName("display_density")]
    public string DisplayDensity { get; init; } = "xxhdpi";

    [JsonPropertyName("resolution")]
    public string Resolution { get; init; } = "1320x2868";

    [JsonPropertyName("language")]
    public string Language { get; init; } = "zh";

    [JsonPropertyName("timezone")]
    public int Timezone { get; init; } = 8;

    [JsonPropertyName("access")]
    public string Access { get; init; } = "wifi";

    [JsonPropertyName("openudid")]
    public string OpenUdid { get; init; } = string.Empty;

    [JsonPropertyName("clientudid")]
    public string ClientUdid { get; init; } = string.Empty;

    [JsonPropertyName("cdid")]
    public string Cdid { get; init; } = string.Empty;

    [JsonPropertyName("region")]
    public string Region { get; init; } = "CN";

    [JsonPropertyName("tz_name")]
    public string TzName { get; init; } = "Asia/Shanghai";

    [JsonPropertyName("tz_offset")]
    public int TzOffset { get; init; } = 28800;

    [JsonPropertyName("sim_region")]
    public string SimRegion { get; init; } = "cn";

    [JsonPropertyName("device_platform")]
    public string DevicePlatform { get; init; } = "android";

    [JsonPropertyName("not_request_sender")]
    public int NotRequestSender { get; init; }

    [JsonPropertyName("is_system_app")]
    public int IsSystemApp { get; init; }

    [JsonPropertyName("sdk_flavor")]
    public string SdkFlavor { get; init; } = "china";

    [JsonPropertyName("rom")]
    public string Rom { get; init; } = "Android17";

    [JsonPropertyName("rom_version")]
    public string RomVersion { get; init; } = "CP2A.260605.012";
}
