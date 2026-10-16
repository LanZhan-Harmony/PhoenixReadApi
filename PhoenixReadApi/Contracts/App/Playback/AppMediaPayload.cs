using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

/// <summary>视频播放模型接口的请求体。</summary>
internal sealed class AppMediaPayload
{
    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("content_type")]
    public int ContentType { get; init; } = 1;

    [JsonPropertyName("biz_param")]
    public AppMediaBusinessParameter BusinessParameter { get; init; } = new();
}

internal sealed class AppMediaBusinessParameter
{
    [JsonPropertyName("need_all_video_definition")]
    public bool NeedAllVideoDefinition { get; init; } = true;

    [JsonPropertyName("video_platform")]
    public int VideoPlatform { get; init; } = 3;
}
