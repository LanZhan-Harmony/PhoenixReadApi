using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

internal sealed class AppSinglePlaybackPayload
{
    [JsonPropertyName("biz_param")]
    public AppSinglePlaybackBizParam BizParam { get; init; } = new();

    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("content_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ContentType { get; init; }
}

internal sealed class AppSinglePlaybackBizParam
{
    [JsonPropertyName("need_all_video_definition")]
    public bool NeedAllVideoDefinition { get; init; } = true;

    [JsonPropertyName("video_id_type")]
    public int VideoIdType { get; init; }

    [JsonPropertyName("video_platform")]
    public int VideoPlatform { get; init; } = 3;
}
