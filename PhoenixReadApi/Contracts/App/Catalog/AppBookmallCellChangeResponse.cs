using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Catalog;

/// <summary>书城瀑布流 cell/change 动态翻页响应模型。</summary>
internal sealed class AppBookmallCellChangeResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public AppBookmallCellChangeData Data { get; init; } = new();
}

internal sealed class AppBookmallCellChangeData
{
    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("cell_view")]
    public AppBookmallCellViewDto CellView { get; init; } = new();
}

internal sealed class AppBookmallCellViewDto
{
    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> VideoData { get; init; } = [];

    [JsonPropertyName("cell_data")]
    public IReadOnlyList<AppBookmallCellDto> CellData { get; init; } = [];

    [JsonPropertyName("book_data")]
    [JsonConverter(typeof(FlexibleSearchBookListConverter))]
    public IReadOnlyList<AppSearchBookDto> BookData { get; init; } = [];
}
