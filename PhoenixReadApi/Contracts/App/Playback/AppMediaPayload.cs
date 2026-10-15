using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

internal sealed class AppMediaPayload
{
    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("content_type")]
    public int ContentType { get; init; } = 1;

    [JsonPropertyName("biz_param")]
    public AppMediaBusinessParam BusinessParameter { get; init; } = new();
}
