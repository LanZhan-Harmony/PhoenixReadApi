using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Web.Search;

internal sealed class WebSearchPageDto
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; init; }

    [JsonPropertyName("query")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Query { get; init; }

    [JsonPropertyName("searchList")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<AppDramaDto> Items { get; init; } = [];

    [JsonPropertyName("totalCount")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int TotalCount { get; init; }
}
