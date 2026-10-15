using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

internal sealed class PlaybackReference
{
    [JsonPropertyName("content_type")]
    public int ContentType { get; init; } = 1004;

    [JsonPropertyName("from_video_id")]
    public string FromVideoId { get; init; } = string.Empty;

    [JsonPropertyName("series_id")]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("vid")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("video_platform")]
    public int VideoPlatform { get; init; } = 3;
}
