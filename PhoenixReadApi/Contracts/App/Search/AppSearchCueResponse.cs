using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Search;

internal sealed class AppSearchCueResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("data_v1")]
    public IReadOnlyList<AppSearchCueItemDto> DataV1 { get; init; } = [];

    [JsonPropertyName("data")]
    public AppSearchCueItemDto Data { get; init; } = new();
}

internal sealed class AppSearchCueItemDto
{
    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Text { get; init; } = string.Empty;
}
