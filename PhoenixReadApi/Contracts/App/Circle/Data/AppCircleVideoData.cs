using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Circle;

internal sealed class AppCircleVideoItem
{
    [JsonPropertyName("video_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("common")]
    public AppCircleVideoCommon Common { get; init; } = new();

    [JsonPropertyName("stat")]
    public AppCircleVideoStat Stat { get; init; } = new();

    [JsonPropertyName("expand")]
    public AppCircleVideoExpand Expand { get; init; } = new();
}

internal sealed class AppCircleVideoCommon
{
    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Introduction { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("create_timestamp")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CreatedAt { get; init; }

    [JsonPropertyName("user_info")]
    public AppCommentUserInfo UserInfo { get; init; } = new();
}

internal sealed class AppCircleVideoStat
{
    [JsonPropertyName("digg_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long DiggCount { get; init; }

    [JsonPropertyName("play_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long PlayCount { get; init; }

    [JsonPropertyName("followed_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long FollowedCount { get; init; }
}

internal sealed class AppCircleVideoExpand
{
    [JsonPropertyName("videos")]
    public IReadOnlyList<AppCircleRelatedDrama> Videos { get; init; } = [];
}

internal sealed class AppCircleVideoDetailData
{
    [JsonPropertyName("video_data")]
    public AppCircleVideoDetailDataContainer VideoData { get; init; } = new();

    [JsonPropertyName("video_list")]
    public IReadOnlyList<AppCircleVideoListItem> VideoList { get; init; } = [];
}

internal sealed class AppCircleVideoDetailDataContainer
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

    [JsonPropertyName("duration")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long DurationSeconds { get; init; }

    [JsonPropertyName("create_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CreatedAt { get; init; }

    [JsonPropertyName("digg_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long LikeCount { get; init; }

    [JsonPropertyName("series_play_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long PlayCount { get; init; }

    [JsonPropertyName("followed_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long FollowedCount { get; init; }

    [JsonPropertyName("ugc_user_info")]
    public AppCommentUserInfo UgcUserInfo { get; init; } = new();

    [JsonPropertyName("video_detail_list")]
    public IReadOnlyList<AppCircleVideoSubDetail> VideoDetailList { get; init; } = [];
}

internal sealed class AppCircleVideoSubDetail
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("series_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("series_cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("series_intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Introduction { get; init; } = string.Empty;

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("series_play_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long PlayCount { get; init; }

    [JsonPropertyName("celebrities")]
    public IReadOnlyList<DramaCelebrityDto> Celebrities { get; init; } = [];
}

internal sealed class AppCircleVideoListItem
{
    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("duration")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long DurationSeconds { get; init; }

    [JsonPropertyName("digged_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long DiggedCount { get; init; }
}
