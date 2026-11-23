using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

internal sealed class AppPlaybackPayload
{
    [JsonPropertyName("biz_param")]
    public AppPlaybackBizParam BizParam { get; init; } = new();

    [JsonPropertyName("mixed_video_id_map")]
    public Dictionary<string, List<string>> MixedVideoIdMap { get; init; } = [];

    [JsonPropertyName("dr_scene")]
    public string DrScene { get; init; } = "player";

    [JsonPropertyName("video_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VideoId { get; init; }

    [JsonPropertyName("content_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ContentType { get; init; }
}

internal sealed class AppPlaybackBizParam
{
    [JsonPropertyName("detail_page_version")]
    public int DetailPageVersion { get; init; }

    [JsonPropertyName("device_level")]
    public int DeviceLevel { get; init; } = 1;

    [JsonPropertyName("disable_digg_stat")]
    public bool DisableDiggStat { get; init; }

    [JsonPropertyName("disable_video_relate_book")]
    public bool DisableVideoRelateBook { get; init; }

    [JsonPropertyName("need_all_video_definition")]
    public bool NeedAllVideoDefinition { get; init; } = true;

    [JsonPropertyName("need_mp4_align")]
    public bool NeedMp4Align { get; init; }

    [JsonPropertyName("use_os_player")]
    public bool UseOsPlayer { get; init; }

    [JsonPropertyName("use_server_dns")]
    public bool UseServerDns { get; init; }

    [JsonPropertyName("video_platform")]
    public int VideoPlatform { get; init; } = 1024;
}
