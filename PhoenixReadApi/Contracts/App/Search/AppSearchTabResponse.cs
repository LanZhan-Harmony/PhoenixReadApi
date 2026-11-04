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

/// <summary>单个搜索 Tab 项（如短剧 Tab 29、小说 Tab 1、用户 Tab 27 等）。</summary>
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

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("data")]
    public IReadOnlyList<AppSearchTabCell> Data { get; init; } = [];
}

/// <summary>搜索结果单元格，包含视频数据、小说、明星演职人员等列表。</summary>
internal sealed class AppSearchTabCell
{
    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> VideoData { get; init; } = [];

    [JsonPropertyName("book_data")]
    [JsonConverter(typeof(FlexibleSearchBookListConverter))]
    public IReadOnlyList<AppSearchBookDto> BookData { get; init; } = [];

    [JsonPropertyName("celebrity_user_list")]
    public IReadOnlyList<AppSearchUserDto> CelebrityUserList { get; init; } = [];
}

/// <summary>搜索结果中的原著小说 DTO。</summary>
internal sealed class AppSearchBookDto
{
    [JsonPropertyName("book_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("book_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookName { get; init; } = string.Empty;

    [JsonPropertyName("abstract")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Abstract { get; init; } = string.Empty;

    [JsonPropertyName("author")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Author { get; init; } = string.Empty;

    [JsonPropertyName("thumb_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("serial_count")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int SerialCount { get; init; }

    [JsonPropertyName("read_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReadCount { get; init; }
}

/// <summary>搜索结果中的明星、创作者或演员用户 DTO。</summary>
internal sealed class AppSearchUserDto
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("base_info")]
    public AppSearchUserBaseInfo BaseInfo { get; init; } = new();
}

internal sealed class AppSearchUserBaseInfo
{
    [JsonPropertyName("user_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("avatar_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string AvatarUrl { get; init; } = string.Empty;
}
