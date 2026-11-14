using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Drama;

internal sealed class AppDetailPayload
{
    [JsonPropertyName("series_id")]
    public string SeriesId { get; init; } = string.Empty;
}
