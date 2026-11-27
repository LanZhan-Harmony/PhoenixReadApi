using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

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

    [JsonPropertyName("player_accumulate_total_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long AccumulatedPlayTimeMs { get; init; }

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

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("pugc_user_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string PugcUserName { get; init; } = string.Empty;

    [JsonPropertyName("pugc_user_avatar")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string PugcUserAvatar { get; init; } = string.Empty;

    [JsonPropertyName("digged_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long DiggCount { get; init; }

    [JsonPropertyName("series_play_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long PlayCount { get; init; }

    [JsonPropertyName("listen_book_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ListenBookId { get; init; } = string.Empty;

    [JsonPropertyName("genre_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int GenreType { get; init; }
}
