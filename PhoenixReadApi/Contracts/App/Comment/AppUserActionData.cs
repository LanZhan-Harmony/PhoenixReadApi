using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppUserShelfData
{
    [JsonPropertyName("video_shelf_info")]
    public IReadOnlyList<AppUserShelfItem> Items { get; init; } = [];

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Total { get; init; }
}

internal sealed class AppUserShelfItem
{
    [JsonPropertyName("video_data")]
    public AppUserShelfVideoData Video { get; init; } = new();

    [JsonPropertyName("series_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesTitle { get; init; } = string.Empty;

    [JsonPropertyName("series_cover_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("series_intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Introduction { get; init; } = string.Empty;

    [JsonPropertyName("series_status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesStatus { get; init; } = string.Empty;

    [JsonPropertyName("category_schema")]
    [JsonConverter(typeof(CategorySchemaConverter))]
    public IReadOnlyList<string> Tags { get; init; } = [];
}

internal sealed class AppUserShelfVideoData
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
    public int Episode { get; init; }
}

internal sealed class AppUserActionData
{
    [JsonPropertyName("mixed_data_list")]
    public IReadOnlyList<AppUserActionRecord> Items { get; init; } = [];

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Total { get; init; }
}

internal sealed class AppUserActionRecord
{
    [JsonPropertyName("data_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int DataType { get; init; }

    [JsonPropertyName("video_data")]
    public AppUserActionVideoData Video { get; init; } = new();
}

internal sealed class AppUserActionVideoData
{
    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("video_desc")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("vid_index")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Episode { get; init; }

    [JsonPropertyName("user_digg")]
    public bool IsLiked { get; init; }

    [JsonPropertyName("user_digg_timestamp_ms")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long LikedAtMilliseconds { get; init; }

    [JsonPropertyName("video_detail")]
    public AppUserActionSeriesDetail Series { get; init; } = new();
}

internal sealed class AppUserActionSeriesDetail
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("series_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("series_intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Introduction { get; init; } = string.Empty;

    [JsonPropertyName("series_cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("series_status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("category_schema")]
    [JsonConverter(typeof(CategorySchemaConverter))]
    public IReadOnlyList<string> Tags { get; init; } = [];
}
