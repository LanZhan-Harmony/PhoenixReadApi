using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppShelfUpdateItem
{
    [JsonPropertyName("series_id")]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("vid")]
    public string VideoId { get; init; } = string.Empty;
}
