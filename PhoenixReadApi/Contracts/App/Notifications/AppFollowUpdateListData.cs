using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Notifications;

internal sealed class AppFollowUpdateListData
{
    [JsonPropertyName("comment_list_info")]
    public AppFollowUpdateListInfo ListInfo { get; init; } = new();

    [JsonPropertyName("mix_data")]
    public IReadOnlyList<AppFollowUpdateRecord> Items { get; init; } = [];
}

internal sealed class AppFollowUpdateListInfo
{
    [JsonPropertyName("cursor")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }
}

internal sealed class AppFollowUpdateRecord
{
    [JsonPropertyName("video")]
    public AppFollowUpdateVideo Video { get; init; } = new();
}

internal sealed class AppFollowUpdateVideo
{
    [JsonPropertyName("video_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("common")]
    public AppFollowUpdateCommon Common { get; init; } = new();

    [JsonPropertyName("expand")]
    public AppFollowUpdateExpand Expand { get; init; } = new();
}

internal sealed class AppFollowUpdateCommon
{
    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("video_content_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ContentType { get; init; }
}

internal sealed class AppFollowUpdateExpand
{
    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("latest_watched_vid_index")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int LatestWatchedIndex { get; init; }

    [JsonPropertyName("has_watched_latest_episode")]
    public bool HasWatchedLatest { get; init; }

    [JsonPropertyName("series_status")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int SeriesStatus { get; init; }

    [JsonPropertyName("extra")]
    public AppFollowUpdateExtra Extra { get; init; } = new();

    [JsonPropertyName("update_info")]
    public AppFollowUpdateInfo UpdateInfo { get; init; } = new();
}

internal sealed class AppFollowUpdateExtra
{
    [JsonPropertyName("follow_update_time_stamp")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long FollowAt { get; init; }

    [JsonPropertyName("max_chapter_first_pass_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long LastPassAt { get; init; }
}

internal sealed class AppFollowUpdateInfo
{
    [JsonPropertyName("show_update_frequency")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Frequency { get; init; } = string.Empty;
}
