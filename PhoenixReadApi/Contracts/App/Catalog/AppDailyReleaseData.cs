using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Catalog;

/// <summary>上新日历接口顶层响应。</summary>
internal sealed class AppDailyReleaseResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public AppDailyReleaseData Data { get; init; } = new();
}

internal sealed class AppDailyReleaseData
{
    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("calendar_schema")]
    public AppDailyReleaseCalendarSchemaDto CalendarSchema { get; init; } = new();

    [JsonPropertyName("subscribe_items")]
    public IReadOnlyList<AppDailyReleaseItemDto> SubscribeItems { get; init; } = [];
}

internal sealed class AppDailyReleaseCalendarSchemaDto
{
    [JsonPropertyName("default_date")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string DefaultDate { get; init; } = string.Empty;

    [JsonPropertyName("date_list")]
    public IReadOnlyList<string> DateList { get; init; } = [];
}

internal sealed class AppDailyReleaseItemDto
{
    [JsonPropertyName("is_online")]
    public bool IsOnline { get; init; }

    [JsonPropertyName("item_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ItemId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("item_desc")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ItemDesc { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cover { get; init; } = string.Empty;

    [JsonPropertyName("sub_info")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SubInfo { get; init; } = string.Empty;

    [JsonPropertyName("schedule_publish_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long SchedulePublishTime { get; init; }

    [JsonPropertyName("expected_publish_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ExpectedPublishTime { get; init; }

    [JsonPropertyName("categories")]
    public IReadOnlyList<string> Categories { get; init; } = [];

    [JsonPropertyName("subscribe_data")]
    public AppDailyReleaseSubscribeDataDto? SubscribeData { get; init; }
}

internal sealed class AppDailyReleaseSubscribeDataDto
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("video_desc")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoDesc { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cover { get; init; } = string.Empty;

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }
}
