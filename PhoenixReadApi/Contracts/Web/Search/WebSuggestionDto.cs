using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Web.Search;

internal sealed class WebSuggestionDto
{
    [JsonPropertyName("display_words")]
    public IReadOnlyList<string> DisplayWords { get; init; } = [];

    [JsonPropertyName("keyword")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Keyword { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("video_data")]
    public DramaDto VideoData { get; init; } = new();

    [JsonPropertyName("word_type")]
    public string WordType { get; init; } = string.Empty;
}
