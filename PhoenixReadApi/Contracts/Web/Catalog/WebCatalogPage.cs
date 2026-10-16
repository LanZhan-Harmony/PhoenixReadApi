using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Web.Catalog;

internal sealed class WebCategoryPageDto
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; init; }

    [JsonPropertyName("recommendList")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> Items { get; init; } = [];

    [JsonPropertyName("pagination")]
    public WebPaginationDto Pagination { get; init; } = new();
}

internal sealed class WebPaginationDto
{
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; init; }
}
