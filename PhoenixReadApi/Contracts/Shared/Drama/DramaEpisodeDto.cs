using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Shared.Drama;

/// <summary>详情响应中的分集记录。</summary>
internal sealed class DramaEpisodeDto
{
    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("vid_index")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Episode { get; init; }

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("duration")]
    [JsonConverter(typeof(FlexibleDoubleConverter))]
    public double DurationSeconds { get; init; }

    [JsonPropertyName("disable_play")]
    public bool DisablePlay { get; init; }

    [JsonPropertyName("vertical")]
    public bool Vertical { get; init; }

    [JsonPropertyName("is_newly_update")]
    public bool IsNewlyUpdate { get; init; }

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cover { get; init; } = string.Empty;

    [JsonPropertyName("episode_cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string EpisodeCover { get; init; } = string.Empty;

    [JsonPropertyName("comment_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CommentCount { get; init; }

    [JsonPropertyName("digged_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long DiggedCount { get; init; }

    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonIgnore]
    public string EffectiveCover =>
        !string.IsNullOrWhiteSpace(Cover) ? Cover : EpisodeCover;
}
