using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Web;

internal sealed class WebDetailsPageDto
{
    [JsonPropertyName("seriesDetail")]
    public WebSeriesDetailDto SeriesDetail { get; init; } = new();
}

internal sealed class WebSeriesDetailDto
{
    [JsonPropertyName("series_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesName { get; init; } = string.Empty;

    [JsonPropertyName("series_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesTitle { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("vid_list")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public IReadOnlyList<string> VideoIds { get; init; } = [];
}
