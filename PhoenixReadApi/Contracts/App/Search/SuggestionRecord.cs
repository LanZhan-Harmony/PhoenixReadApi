using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Search;

internal sealed class SuggestionRecord
{
    [JsonPropertyName("display_words")]
    public IReadOnlyList<string> DisplayWords { get; init; } = [];

    [JsonPropertyName("keyword")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Keyword { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("video_data")]
    public AppDramaDto? VideoData { get; init; }

    [JsonPropertyName("word_type")]
    public string WordType { get; init; } = string.Empty;
}
