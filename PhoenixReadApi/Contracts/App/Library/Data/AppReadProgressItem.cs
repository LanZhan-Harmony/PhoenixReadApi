using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppReadProgressItem
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("vid_index")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeNumber { get; init; }

    [JsonPropertyName("episode_count")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("last_watch_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long LastWatchTime { get; init; }

    [JsonPropertyName("current_play_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CurrentPlayTime { get; init; }

    [JsonPropertyName("current_total_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CurrentTotalTime { get; init; }

    [JsonPropertyName("player_cumulative_total_duration")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CumulativeDuration { get; init; }
}
