using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Web.Search;

internal sealed class WebSuggestionResponseDto
{
    [JsonPropertyName("suggest_list")]
    public IReadOnlyList<WebSuggestionDto> SuggestList { get; init; } = [];
}
