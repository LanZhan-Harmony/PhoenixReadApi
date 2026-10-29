using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Search;

/// <summary>短剧综合搜索 tab/v 接口的顶层响应。</summary>
internal sealed class AppSearchTabResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("BaseResp")]
    public AppBaseResponse BaseResponse { get; init; } = new();

    [JsonPropertyName("search_tabs")]
    public IReadOnlyList<AppSearchTabItem> SearchTabs { get; init; } = [];

    [JsonPropertyName("data")]
    public AppSearchTabData Data { get; init; } = new();
}

/// <summary>嵌套在 data 字段下的 search_tabs 容器。</summary>
internal sealed class AppSearchTabData
{
    [JsonPropertyName("search_tabs")]
    public IReadOnlyList<AppSearchTabItem> SearchTabs { get; init; } = [];
}

/// <summary>单个搜索 Tab 项（如短剧 Tab 29）。</summary>
internal sealed class AppSearchTabItem
{
    [JsonPropertyName("tab_type")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string TabType { get; init; } = string.Empty;

    [JsonPropertyName("search_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SearchId { get; init; } = string.Empty;

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("data")]
    public IReadOnlyList<AppSearchTabCell> Data { get; init; } = [];
}

/// <summary>搜索结果单元格，包含视频数据列表。</summary>
internal sealed class AppSearchTabCell
{
    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> VideoData { get; init; } = [];
}
