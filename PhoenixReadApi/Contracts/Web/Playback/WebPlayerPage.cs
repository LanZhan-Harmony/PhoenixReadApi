using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.Web.Details;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Web.Playback;

internal sealed class WebPlayerPageDto
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; init; }

    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("protectedPlayback")]
    public bool ProtectedPlayback { get; init; }

    [JsonPropertyName("publicVideoSeo")]
    public WebPublicVideoSeoDto PublicVideoSeo { get; init; } = new();

    [JsonPropertyName("seriesDetail")]
    public WebSeriesDetailDto SeriesDetail { get; init; } = new();
}

internal sealed class WebPublicVideoSeoDto
{
    [JsonPropertyName("available")]
    public bool Available { get; init; }

    [JsonPropertyName("durationSeconds")]
    [JsonConverter(typeof(FlexibleDoubleConverter))]
    public double DurationSeconds { get; init; }

    [JsonPropertyName("seriesId")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("itemId")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ItemId { get; init; } = string.Empty;

    [JsonPropertyName("posterUrl")]
    public string PosterUrl { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }
}
