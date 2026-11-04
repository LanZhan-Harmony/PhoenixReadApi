using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Catalog;

/// <summary>短剧动态筛选接口请求载荷。</summary>
internal sealed class AppShortFilterPayload
{
    [JsonPropertyName("req_scene")]
    public string ReqScene { get; init; } = "default";

    [JsonPropertyName("req_type")]
    public string ReqType { get; init; } = "only_panel";

    [JsonPropertyName("need_selector_panel")]
    public bool NeedSelectorPanel { get; init; }

    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 18;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("filter_ids")]
    public string FilterIds { get; init; } = string.Empty;

    [JsonPropertyName("select_items")]
    public IReadOnlyDictionary<string, IReadOnlyList<string>> SelectItems { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();
}

/// <summary>短剧动态筛选接口响应模型。</summary>
internal sealed class AppShortFilterResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public AppShortFilterData Data { get; init; } = new();
}

internal sealed class AppShortFilterData
{
    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("selector_rows")]
    public IReadOnlyList<AppShortFilterRowDto> SelectorRows { get; init; } = [];

    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> VideoData { get; init; } = [];
}

internal sealed class AppShortFilterRowDto
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("row_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string RowName { get; init; } = string.Empty;

    [JsonPropertyName("items")]
    public IReadOnlyList<AppShortFilterItemDto> Items { get; init; } = [];
}

internal sealed class AppShortFilterItemDto
{
    [JsonPropertyName("show_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ShowName { get; init; } = string.Empty;

    [JsonPropertyName("selector_item_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SelectorItemId { get; init; } = string.Empty;
}
