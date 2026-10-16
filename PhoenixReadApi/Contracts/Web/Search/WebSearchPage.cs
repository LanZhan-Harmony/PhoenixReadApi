using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Web.Search;

internal sealed class WebSearchPageDto
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; init; }

    [JsonPropertyName("query")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Query { get; init; } = string.Empty;

    [JsonPropertyName("searchList")]
    public IReadOnlyList<WebSearchRecordDto> Items { get; init; } = [];

    [JsonPropertyName("totalCount")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int TotalCount { get; init; }
}

internal sealed class WebSearchRecordDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("keyword")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Keyword { get; init; } = string.Empty;

    [JsonPropertyName("video_data")]
    public DramaDto VideoData { get; init; } = new();
}
