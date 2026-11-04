using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Search;

internal sealed class AppHotSearchPlanResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public IReadOnlyList<AppHotSearchPlanCellDto> Data { get; init; } = [];
}

internal sealed class AppHotSearchPlanCellDto
{
    [JsonPropertyName("show_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ShowType { get; init; }

    [JsonPropertyName("cell_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CellName { get; init; } = string.Empty;

    [JsonPropertyName("lynx_data")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string LynxData { get; init; } = string.Empty;
}

internal sealed class AppHotSearchLynxCellDto
{
    [JsonPropertyName("cell_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CellName { get; init; } = string.Empty;

    [JsonPropertyName("show_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ShowType { get; init; }

    [JsonPropertyName("video_data")]
    public IReadOnlyList<AppHotSearchVideoDto> VideoData { get; init; } = [];

    [JsonPropertyName("video_topic")]
    public IReadOnlyList<AppHotSearchTopicDto> VideoTopics { get; init; } = [];

    [JsonPropertyName("cell_data")]
    public IReadOnlyList<AppHotSearchLynxCellDto> CellData { get; init; } = [];
}

internal sealed class AppHotSearchVideoDto
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cover { get; init; } = string.Empty;

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("card_tips")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CardTips { get; init; } = string.Empty;

    [JsonPropertyName("rec_text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string RecText { get; init; } = string.Empty;

    [JsonPropertyName("tag_info")]
    public AppHotSearchTagDto TagInfo { get; init; } = new();
}

internal sealed class AppHotSearchTagDto
{
    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Text { get; init; } = string.Empty;
}

internal sealed class AppHotSearchTopicDto
{
    [JsonPropertyName("topic_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string TopicId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("secondary_info_list")]
    public IReadOnlyList<AppHotSearchTopicInfoDto> SecondaryInfoList { get; init; } = [];
}

internal sealed class AppHotSearchTopicInfoDto
{
    [JsonPropertyName("content")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Content { get; init; } = string.Empty;
}
