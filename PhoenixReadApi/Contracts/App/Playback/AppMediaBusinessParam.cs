using System.Text.Json.Serialization;

namespace PhoenixReadApi.Contracts.App.Playback;

internal sealed class AppMediaBusinessParam
{
    [JsonPropertyName("need_all_video_definition")]
    public bool NeedAllVideoDefinition { get; init; } = true;

    [JsonPropertyName("video_platform")]
    public int VideoPlatform { get; init; } = 3;
}
