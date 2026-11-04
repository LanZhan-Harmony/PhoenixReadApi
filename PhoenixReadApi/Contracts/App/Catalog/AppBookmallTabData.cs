using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Catalog;

/// <summary>首页频道发现流 Tab 响应模型。</summary>
internal sealed class AppBookmallTabResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public AppBookmallTabData Data { get; init; } = new();
}

internal sealed class AppBookmallTabData
{
    [JsonPropertyName("tab_item")]
    public IReadOnlyList<AppBookmallTabItemDto> TabItems { get; init; } = [];
}

internal sealed class AppBookmallTabItemDto
{
    [JsonPropertyName("tab_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int TabType { get; init; }

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("client_template")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ClientTemplate { get; init; } = string.Empty;

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("cell_data")]
    public IReadOnlyList<AppBookmallCellDto> CellData { get; init; } = [];
}

internal sealed class AppBookmallCellDto
{
    [JsonPropertyName("cell_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CellId { get; init; } = string.Empty;

    [JsonPropertyName("cell_id_str")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CellIdStr { get; init; } = string.Empty;

    [JsonPropertyName("show_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ShowType { get; init; }

    [JsonPropertyName("cell_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CellName { get; init; } = string.Empty;

    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> VideoData { get; init; } = [];

    [JsonPropertyName("book_data")]
    [JsonConverter(typeof(FlexibleSearchBookListConverter))]
    public IReadOnlyList<AppSearchBookDto> BookData { get; init; } = [];

    [JsonPropertyName("cell_data")]
    public IReadOnlyList<AppBookmallCellDto> CellData { get; init; } = [];
}
