using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppShelfVideoData
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("vid_index")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeNumber { get; init; }
}
