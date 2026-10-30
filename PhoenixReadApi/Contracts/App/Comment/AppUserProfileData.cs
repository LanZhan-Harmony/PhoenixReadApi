using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppUserProfileData
{
    [JsonPropertyName("common_list_info")]
    public AppUserListInfo ListInfo { get; init; } = new();

    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppUserWorkRecord> Items { get; init; } = [];

    [JsonPropertyName("tabs")]
    public IReadOnlyList<AppUserProfileTab> Tabs { get; init; } = [];

    [JsonPropertyName("user_info")]
    public AppCommentUserInfo UserInfo { get; init; } = new();
}

internal sealed class AppUserListInfo
{
    [JsonPropertyName("cursor")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }
}

internal sealed class AppUserProfileTab
{
    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("tab_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Type { get; init; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Total { get; init; }
}

internal sealed class AppUserWorkRecord
{
    [JsonPropertyName("data_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int DataType { get; init; }

    [JsonPropertyName("video")]
    public AppUserVideo Video { get; init; } = new();
}

internal sealed class AppUserVideo
{
    [JsonPropertyName("video_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("common")]
    public AppUserVideoCommon Common { get; init; } = new();

    [JsonPropertyName("expand")]
    public AppUserVideoExpand Expand { get; init; } = new();

    [JsonPropertyName("stat")]
    public AppUserVideoStat Stat { get; init; } = new();
}

internal sealed class AppUserVideoCommon
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

internal sealed class AppUserVideoExpand
{
    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }
}

internal sealed class AppUserVideoStat
{
    [JsonPropertyName("digg_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long LikeCount { get; init; }

    [JsonPropertyName("play_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ViewCount { get; init; }
}
