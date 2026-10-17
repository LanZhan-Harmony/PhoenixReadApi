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

    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;
}
