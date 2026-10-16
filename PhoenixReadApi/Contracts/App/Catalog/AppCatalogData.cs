using System.Text.Json;
using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;

namespace PhoenixReadApi.Internal.Contracts.App.Catalog;

internal sealed class AppCatalogData
{
    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> Items { get; init; } = [];

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;
}
