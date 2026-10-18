using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppHistoryData
{
    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppHistoryItem> Items { get; init; } = [];

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Total { get; init; }

    [JsonPropertyName("online_total_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long OnlineTotalCount { get; init; }

    [JsonPropertyName("offline_total_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long OfflineTotalCount { get; init; }
}

internal sealed class AppHistoryItem
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("book_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("book_id_str")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookIdString { get; init; } = string.Empty;

    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("video_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoIdAlias { get; init; } = string.Empty;

    [JsonPropertyName("vid_index")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeNumber { get; init; }

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("book_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookName { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("thumb_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ThumbUrl { get; init; } = string.Empty;

    [JsonPropertyName("current_play_position")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CurrentPositionMs { get; init; }

    [JsonPropertyName("current_play_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CurrentPositionAlias { get; init; }

    [JsonPropertyName("duration")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long DurationMs { get; init; }

    [JsonPropertyName("last_watch_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long UpdatedAtUnixMilliseconds { get; init; }

    [JsonPropertyName("read_timestamp_ms")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReadTimestampMilliseconds { get; init; }

    [JsonPropertyName("video_data")]
    public AppHistoryVideoData Video { get; init; } = new();
}

internal sealed class AppHistoryVideoData
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("vid_index")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeNumber { get; init; }

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;
}

internal sealed class AppReadProgressEnvelope
{
    [JsonPropertyName("video_data")]
    public IReadOnlyList<AppReadProgressItem> Items { get; init; } = [];

    [JsonPropertyName("local_book_data")]
    public IReadOnlyList<AppReadProgressItem> LocalItems { get; init; } = [];

    [JsonPropertyName("data")]
    public IReadOnlyList<AppReadProgressItem> DataItems { get; init; } = [];
}

internal sealed class AppReadProgressItem
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("vid_index")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeNumber { get; init; }

    [JsonPropertyName("episode_count")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("last_watch_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long LastWatchTime { get; init; }

    [JsonPropertyName("current_play_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CurrentPlayTime { get; init; }

    [JsonPropertyName("current_total_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CurrentTotalTime { get; init; }

    [JsonPropertyName("player_cumulative_total_duration")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CumulativeDuration { get; init; }
}

internal sealed class AppShelfListData
{
    [JsonPropertyName("video_shelf_info")]
    public IReadOnlyList<AppShelfItem> ShelfItems { get; init; } = [];

    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppShelfItem> Items { get; init; } = [];

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Total { get; init; }
}

internal sealed class AppShelfItem
{
    [JsonPropertyName("video_data")]
    public AppShelfVideoData Video { get; init; } = new();

    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("series_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("series_cover_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("series_intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Description { get; init; } = string.Empty;
}

internal sealed class AppShelfVideoData
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("vid_index")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeNumber { get; init; }
}

internal sealed class AppFollowData
{
    [JsonPropertyName("mix_data")]
    public IReadOnlyList<AppFollowItem> Items { get; init; } = [];

    [JsonPropertyName("comment_list_info")]
    public AppFollowListInfo ListInfo { get; init; } = new();
}

internal sealed class AppFollowListInfo
{
    [JsonPropertyName("cursor")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }
}

internal sealed class AppFollowItem
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("create_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CreatedAt { get; init; }

    [JsonPropertyName("video_data")]
    public AppFollowVideo Video { get; init; } = new();
}

internal sealed class AppFollowVideo
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;
}

internal sealed class AppReservationData
{
    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppReservationItem> Items { get; init; } = [];

    [JsonPropertyName("subscribe_items")]
    public IReadOnlyList<AppReservationItem> SubscribeItems { get; init; } = [];

    [JsonPropertyName("subscribe_list")]
    public IReadOnlyList<AppReservationItem> LegacySubscribeItems { get; init; } = [];

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Total { get; init; }

    [JsonPropertyName("online_total_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long OnlineTotalCount { get; init; }

    [JsonPropertyName("offline_total_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long OfflineTotalCount { get; init; }
}

internal sealed class AppReservationItem
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("book_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("item_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ItemId { get; init; } = string.Empty;

    [JsonPropertyName("video_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("item_desc")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("create_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReservedAt { get; init; }

    [JsonPropertyName("publish_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReleaseAt { get; init; }

    [JsonPropertyName("expected_publish_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ExpectedPublishAt { get; init; }

    [JsonPropertyName("is_online")]
    public bool IsOnline { get; init; }
}

internal sealed class AppShareData
{
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("short_url")]
    public string ShortUrl { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;
}
