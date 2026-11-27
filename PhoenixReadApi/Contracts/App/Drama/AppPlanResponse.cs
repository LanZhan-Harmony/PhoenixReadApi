using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Drama;

/// <summary>红果通用 Plan Feed (bookapi/plan/v1) 响应模型。</summary>
internal sealed class AppPlanResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("BaseResp")]
    public AppBaseResponse BaseResponse { get; init; } = new();

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public IReadOnlyList<AppPlanCellDto> Data { get; init; } = [];
}

/// <summary>Plan Feed 单元格模型。</summary>
internal sealed class AppPlanCellDto
{
    [JsonPropertyName("show_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ShowType { get; init; }

    [JsonPropertyName("cell_id_str")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CellIdStr { get; init; } = string.Empty;

    [JsonPropertyName("cell_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CellId { get; init; } = string.Empty;

    [JsonPropertyName("cell_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CellName { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> VideoData { get; init; } = [];

    [JsonPropertyName("book_data")]
    public IReadOnlyList<AppPlanBookDto> BookData { get; init; } = [];

    [JsonPropertyName("cell_data")]
    public IReadOnlyList<AppPlanFeedCellDto> CellData { get; init; } = [];

    [JsonIgnore]
    public string EffectiveCellId => !string.IsNullOrWhiteSpace(CellIdStr) ? CellIdStr : CellId;

    [JsonIgnore]
    public IReadOnlyList<DramaDto> EffectiveItems => VideoData ?? [];
}

/// <summary>Plan Feed 嵌套双列卡片项（短剧或图文帖子）。</summary>
internal sealed class AppPlanFeedCellDto
{
    [JsonPropertyName("show_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ShowType { get; init; }

    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> VideoData { get; init; } = [];

    [JsonPropertyName("post_data")]
    public IReadOnlyList<AppSearchPostDto> PostData { get; init; } = [];
}

/// <summary>Plan Feed 相关原著小说 DTO。</summary>
internal sealed class AppPlanBookDto
{
    [JsonPropertyName("book_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("book_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookName { get; init; } = string.Empty;

    [JsonPropertyName("author")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Author { get; init; } = string.Empty;

    [JsonPropertyName("abstract")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Abstract { get; init; } = string.Empty;

    [JsonPropertyName("thumb_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ThumbUrl { get; init; } = string.Empty;

    [JsonPropertyName("creation_status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CreationStatus { get; init; } = string.Empty;

    [JsonPropertyName("serial_count")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int SerialCount { get; init; }

    [JsonPropertyName("read_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReadCount { get; init; }

    [JsonPropertyName("tags")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Tags { get; init; } = string.Empty;
}

/// <summary>详情页“猜你喜欢”翻页接口 (bookmall/cell/change/v1) 响应数据。</summary>
internal sealed class AppPlanCellChangeData
{
    [JsonPropertyName("cell_view")]
    public AppPlanCellDto? CellView { get; init; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;
}
