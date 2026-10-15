using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Search;

internal sealed class SuggestionData
{
    [JsonPropertyName("suggest_list")]
    public IReadOnlyList<SuggestionRecord> SuggestList { get; init; } = [];
}
