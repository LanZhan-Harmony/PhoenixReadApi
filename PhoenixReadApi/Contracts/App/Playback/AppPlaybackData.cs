using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

internal sealed class AppPlaybackData
{
    [JsonPropertyName("video_model")]
    [JsonConverter(typeof(VideoModelDtoConverter))]
    public AppPlaybackModel VideoModel { get; init; } = new();

    [JsonPropertyName("video_model_data")]
    public Dictionary<string, AppMultiPlaybackEntry>? VideoModelData { get; init; }

    [JsonPropertyName("1004")]
    public Dictionary<string, AppMultiPlaybackEntry>? Group1004 { get; init; }

    [JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? AdditionalData { get; init; }

    [JsonPropertyName("video_list")]
    public IReadOnlyList<AppMediaVariantDto> VideoList { get; init; } = [];

    [JsonPropertyName("video_duration")]
    [JsonConverter(typeof(FlexibleDoubleConverter))]
    public double VideoDuration { get; init; }

    [JsonPropertyName("duration")]
    [JsonConverter(typeof(FlexibleDoubleConverter))]
    public double Duration { get; init; }

    [JsonPropertyName("expire_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ExpireTime { get; init; }
}
