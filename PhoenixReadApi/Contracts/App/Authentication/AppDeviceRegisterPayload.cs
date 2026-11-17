using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Authentication;

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

/// <summary>设备注册 OAID 标识载荷。</summary>
internal sealed class AppDeviceRegisterOaid
{
    [JsonPropertyName("req_id")]
    public string ReqId { get; init; } = string.Empty;

    [JsonPropertyName("hw_id_version_code")]
    public string HwIdVersionCode { get; init; } = "0";

    [JsonPropertyName("take_ms")]
    public string TakeMs { get; init; } = "6";

    [JsonPropertyName("is_track_limited")]
    public string IsTrackLimited { get; init; } = "false";

    [JsonPropertyName("query_times")]
    public string QueryTimes { get; init; } = "1";

    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("time")]
    public string Time { get; init; } = string.Empty;
}

/// <summary>设备注册请求头信息。</summary>
internal sealed class AppDeviceRegisterHeader
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; init; } = "红果免费短剧";

    [JsonPropertyName("update_version_code")]
    public int UpdateVersionCode { get; init; } = 73932;

    [JsonPropertyName("manifest_version_code")]
    public int ManifestVersionCode { get; init; } = 73932;

    [JsonPropertyName("app_version_minor")]
    public string AppVersionMinor { get; init; } = "7.3.9.32";

    [JsonPropertyName("aid")]
    public int Aid { get; init; } = 8662;

    [JsonPropertyName("channel")]
    public string Channel { get; init; } = "update_64";

    [JsonPropertyName("package")]
    public string Package { get; init; } = "com.phoenix.read";

    [JsonPropertyName("app_version")]
    public string AppVersion { get; init; } = "7.3.9.32";

    [JsonPropertyName("version_code")]
    public int VersionCode { get; init; } = 73932;

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

    [JsonPropertyName("language")]
    public string Language { get; init; } = "zh";

    [JsonPropertyName("timezone")]
    public int Timezone { get; init; } = 8;

    [JsonPropertyName("access")]
    public string Access { get; init; } = "wifi";

    [JsonPropertyName("region")]
    public string Region { get; init; } = "CN";

    [JsonPropertyName("tz_name")]
    public string TzName { get; init; } = "Asia/Shanghai";

    [JsonPropertyName("tz_offset")]
    public int TzOffset { get; init; } = 28800;

    [JsonPropertyName("cdid")]
    public string Cdid { get; init; } = string.Empty;

    [JsonPropertyName("openudid")]
    public string OpenUdid { get; init; } = string.Empty;

    [JsonPropertyName("clientudid")]
    public string ClientUdid { get; init; } = string.Empty;

    [JsonPropertyName("ipv6_list")]
    public IReadOnlyList<string> Ipv6List { get; init; } = [];

    [JsonPropertyName("sim_serial_number")]
    public IReadOnlyList<string> SimSerialNumber { get; init; } = [];

    [JsonPropertyName("oaid")]
    public AppDeviceRegisterOaid Oaid { get; init; } = new();

    [JsonPropertyName("oaid_may_support")]
    public bool OaidMaySupport { get; init; } = true;

    [JsonPropertyName("req_id")]
    public string ReqId { get; init; } = string.Empty;

    [JsonPropertyName("device_platform")]
    public string DevicePlatform { get; init; } = "android";

    [JsonPropertyName("guest_mode")]
    public int GuestMode { get; init; }

    [JsonPropertyName("is_system_app")]
    public int IsSystemApp { get; init; }

    [JsonPropertyName("sdk_flavor")]
    public string SdkFlavor { get; init; } = "china";

    [JsonPropertyName("device_id")]
    public string DeviceId { get; init; } = "0";

    [JsonPropertyName("install_id")]
    public string InstallId { get; init; } = "0";
}
