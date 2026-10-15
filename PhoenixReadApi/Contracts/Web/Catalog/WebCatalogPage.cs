using System.Text.Json.Serialization;
using PhoenixReadApi.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Contracts.Web.Catalog;

internal sealed class WebCategoryPageDto
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; init; }

    [JsonPropertyName("recommendList")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<AppDramaDto> Items { get; init; } = [];

    [JsonPropertyName("pagination")]
    public WebPaginationDto? Pagination { get; init; }
}

internal sealed class WebPaginationDto
{
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; init; }
}
