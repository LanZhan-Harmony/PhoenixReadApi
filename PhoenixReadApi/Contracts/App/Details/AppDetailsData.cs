using System.Text.Json.Serialization;
using PhoenixReadApi.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App;

internal sealed class AppDetailsData
{
    [JsonPropertyName("video_data")]
    public AppDramaDto? VideoData { get; init; }

    [JsonPropertyName("video_list")]
    [JsonConverter(typeof(FlexibleVideoListConverter))]
    public IReadOnlyList<AppVideoDto> VideoList { get; init; } = [];
}

internal sealed class AppVideoDto
{
    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? VideoId { get; init; }

    [JsonPropertyName("vid_index")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Episode { get; init; }

    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SeriesId { get; init; }
}
