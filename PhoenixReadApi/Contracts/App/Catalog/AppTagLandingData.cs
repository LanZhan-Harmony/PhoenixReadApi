using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Catalog;

internal sealed class AppTagLandingData
{
    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("selector")]
    public AppTagSelectorContainer Selector { get; init; } = new();

    [JsonPropertyName("order")]
    public AppTagSelectorContainer Order { get; init; } = new();

    [JsonPropertyName("video_info")]
    public IReadOnlyList<AppTagDramaItem> VideoInfo { get; init; } = [];
}

internal sealed class AppTagSelectorContainer
{
    [JsonPropertyName("rows")]
    public IReadOnlyList<AppTagSelectorRowDto> Rows { get; init; } = [];
}

internal sealed class AppTagSelectorRowDto
{
    [JsonPropertyName("row_name")]
    public string RowName { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("items")]
    public IReadOnlyList<AppTagSelectorItemDto> Items { get; init; } = [];
}

internal sealed class AppTagSelectorItemDto
{
    [JsonPropertyName("show_name")]
    public string ShowName { get; init; } = string.Empty;

    [JsonPropertyName("selector_item_id")]
    public string SelectorItemId { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;
}

internal sealed class AppTagDramaItem
{
    [JsonPropertyName("series_id")]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("vid")]
    public string Vid { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("video_desc")]
    public string VideoDesc { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    public string Cover { get; init; } = string.Empty;

    [JsonPropertyName("duration")]
    public int Duration { get; init; }

    [JsonPropertyName("episode_cnt")]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("score")]
    public string Score { get; init; } = string.Empty;

    [JsonPropertyName("sub_title")]
    public string SubTitle { get; init; } = string.Empty;

    [JsonPropertyName("rec_text")]
    public string RecText { get; init; } = string.Empty;

    [JsonPropertyName("sub_title_list")]
    public IReadOnlyList<AppSubTitleItem> SubTitleList { get; init; } = [];
}

internal sealed class AppSubTitleItem
{
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
}
