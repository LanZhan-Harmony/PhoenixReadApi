namespace PhoenixReadApi.Internal.Infrastructure.Runtime;

/// <summary>
/// 集中管理红果客户端统一设备参数、系统版本与 User-Agent，严禁修改设备参数。
/// 所有组件（设备注册、请求查询参数、User-Agent 等）均以此处定义为唯一事实来源，避免不同模块设备参数分化。
/// </summary>
internal static class PhoenixReadDeviceProfile
{
    /// <summary>设备型号：HONOR 荣耀固定机型。</summary>
    public const string DeviceModel = "WKL-AN20";

    /// <summary>设备品牌。</summary>
    public const string DeviceBrand = "HONOR";

    /// <summary>设备生产厂商。</summary>
    public const string DeviceManufacturer = "HONOR";

    /// <summary>设备操作系统平台。</summary>
    public const string DevicePlatform = "android";

    /// <summary>操作系统名称。</summary>
    public const string Os = "Android";

    /// <summary>操作系统版本号。</summary>
    public const string OsVersion = "17";

    /// <summary>操作系统 API 级别。</summary>
    public const int OsApi = 37;

    /// <summary>系统 ROM 版本标识。</summary>
    public const string RomVersion = "CP2A.260605.012";

    /// <summary>构建版本号。</summary>
    public const string BuildId = "CP2A.260605.012";

    /// <summary>屏幕物理分辨率（宽*高）。</summary>
    public const string Resolution = "1320*2868";

    /// <summary>屏幕像素密度 DPI。</summary>
    public const int Dpi = 360;

    /// <summary>CPU 架构指令集。</summary>
    public const string CpuAbi = "arm64-v8a";

    /// <summary>语言设置。</summary>
    public const string Language = "zh";

    /// <summary>区域设置。</summary>
    public const string Region = "CN";

    /// <summary>时区名称。</summary>
    public const string TzName = "Asia/Shanghai";

    /// <summary>时区偏移（秒）。</summary>
    public const int TzOffset = 28800;

    /// <summary>时区（小时）。</summary>
    public const int Timezone = 8;

    /// <summary>网络连接方式。</summary>
    public const string Access = "wifi";

    /// <summary>红果免费短剧应用 ID。</summary>
    public const int Aid = 8662;

    /// <summary>红果免费短剧应用 ID（字符串格式）。</summary>
    public const string AidString = "8662";

    /// <summary>应用名称。</summary>
    public const string AppName = "novelread";

    /// <summary>Android 应用包名。</summary>
    public const string PackageName = "com.phoenix.read";

    /// <summary>应用展示名称。</summary>
    public const string DisplayName = "红果免费短剧";

    /// <summary>主应用版本码。</summary>
    public const int VersionCode = 73932;

    /// <summary>主应用版本名称。</summary>
    public const string VersionName = "7.3.9.32";

    /// <summary>分发渠道。</summary>
    public const string Channel = "update_64";

    /// <summary>SDK 分支类型。</summary>
    public const string SdkFlavor = "china";

    /// <summary>标准客户端 User-Agent。</summary>
    public static readonly string DefaultUserAgent =
        $"com.phoenix.read/{VersionCode} (Linux; U; Android {OsVersion}; zh_CN; {DeviceModel}; Build/{BuildId}; Cronet/TTNetVersion:8d40f833 2026-03-03 QuicVersion:21ac1950 2025-11-18)";

    /// <summary>播放协议专用 User-Agent。</summary>
    public static readonly string PlaybackUserAgent =
        $"com.phoenix.read/{VersionCode} (Linux; U; Android {OsVersion}; zh_CN_#Hans; {DeviceModel}; Build/{BuildId}; tt-ok/3.12.13.20)";

    /// <summary>App 接口统一拼接的前置查询参数串。</summary>
    public static readonly string StaticAppQueryPrefix =
        $"aid={Aid}&app_name={AppName}&version_code={VersionCode}&version_name={VersionName}&manifest_version_code={VersionCode}&update_version_code={VersionCode}&channel={Channel}&device_platform={DevicePlatform}&os=android&ssmix=a&device_type={DeviceModel}&device_brand={DeviceBrand}&language={Language}&os_api={OsApi}&os_version={OsVersion}&resolution={Resolution}&dpi={Dpi}&compliance_status=0&need_personal_recommend=1&player_so_load=1&is_android_pad_screen=0&rom_version={RomVersion}&dragon_device_type=phone&host_abi={CpuAbi}&pv_player={VersionCode}&ac={Access}";
}
