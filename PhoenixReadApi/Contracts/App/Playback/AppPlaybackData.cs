using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

internal sealed class AppPlaybackData
{
    [JsonPropertyName("video_model")]
    [JsonConverter(typeof(VideoModelDtoConverter))]
    public AppPlaybackModel VideoModel { get; init; } = new();
}
