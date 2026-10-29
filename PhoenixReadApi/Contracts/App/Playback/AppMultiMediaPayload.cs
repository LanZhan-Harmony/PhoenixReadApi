using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

/// <summary>多视频播放模型接口请求体。</summary>
internal sealed class AppMultiMediaPayload
{
    [JsonPropertyName("biz_param")]
    public AppMultiMediaBusinessParameter BusinessParameter { get; init; } = new();

    [JsonPropertyName("mixed_video_id_map")]
    public Dictionary<string, List<string>> MixedVideoIdMap { get; init; } = [];
}

/// <summary>多视频播放模型业务参数。</summary>
internal sealed class AppMultiMediaBusinessParameter
{
    [JsonPropertyName("detail_page_version")]
    public int DetailPageVersion { get; init; } = 0;

    [JsonPropertyName("device_level")]
    public int DeviceLevel { get; init; } = 3;

    [JsonPropertyName("disable_digg_stat")]
    public bool DisableDiggStat { get; init; } = false;

    [JsonPropertyName("need_all_video_definition")]
    public bool NeedAllVideoDefinition { get; init; } = true;

    [JsonPropertyName("need_mp4_align")]
    public bool NeedMp4Align { get; init; } = false;

    [JsonPropertyName("use_os_player")]
    public bool UseOsPlayer { get; init; } = false;

    [JsonPropertyName("use_server_dns")]
    public bool UseServerDns { get; init; } = false;

    [JsonPropertyName("video_platform")]
    public int VideoPlatform { get; init; } = 1024;
}
