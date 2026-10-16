using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

/// <summary>多视频播放接口的请求体。</summary>
internal sealed class AppPlaybackPayload
{
    [JsonPropertyName("biz_param")]
    public AppPlaybackBusinessParameter BusinessParameter { get; init; } = new();

    [JsonPropertyName("dr_scene")]
    public string Scene { get; init; } = "preload";

    [JsonPropertyName("mixed_video_id_map")]
    public IReadOnlyDictionary<string, IReadOnlyList<string>> VideoIds { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();
}

internal sealed class AppPlaybackBusinessParameter
{
    [JsonPropertyName("device_level")]
    public int DeviceLevel { get; init; } = 3;

    [JsonPropertyName("need_all_video_definition")]
    public bool NeedAllVideoDefinition { get; init; } = true;

    [JsonPropertyName("video_platform")]
    public int VideoPlatform { get; init; } = 1024;
}

internal sealed class AppPlaybackItem
{
    [JsonPropertyName("video_model")]
    public string VideoModel { get; init; } = string.Empty;
}
