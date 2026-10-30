using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppCommentListData
{
    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppCommentRecord> Items { get; init; } = [];

    [JsonPropertyName("common_list_info")]
    public AppCommentListInfo ListInfo { get; init; } = new();
}

internal sealed class AppCommentListInfo
{
    [JsonPropertyName("cursor")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Total { get; init; }
}

internal sealed class AppCommentRecord
{
    [JsonPropertyName("comment")]
    public AppComment Comment { get; init; } = new();

    [JsonPropertyName("data_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int DataType { get; init; }
}

internal sealed class AppComment
{
    [JsonPropertyName("common")]
    public AppCommentCommon Common { get; init; } = new();

    [JsonPropertyName("expand")]
    public AppCommentExpand Expand { get; init; } = new();

    [JsonPropertyName("comment_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("stat")]
    public AppCommentStat Stat { get; init; } = new();
}

internal sealed class AppCommentCommon
{
    [JsonPropertyName("group_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string GroupId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public AppCommentContent Content { get; init; } = new();

    [JsonPropertyName("create_timestamp")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CreatedAt { get; init; }

    [JsonPropertyName("user_info")]
    public AppCommentUserInfo UserInfo { get; init; } = new();
}

internal sealed class AppCommentContent
{
    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Text { get; init; } = string.Empty;
}

internal sealed class AppCommentUserInfo
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("user_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("user_avatar")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string AvatarUrl { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Introduction { get; init; } = string.Empty;

    [JsonPropertyName("base_info")]
    public AppCommentUserBaseInfo BaseInfo { get; init; } = new();
}

internal sealed class AppCommentUserBaseInfo
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("user_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("user_avatar")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string AvatarUrl { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Introduction { get; init; } = string.Empty;
}

internal sealed class AppCommentExpand
{
    [JsonPropertyName("offset_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long OffsetTime { get; init; }

    [JsonPropertyName("book_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookId { get; init; } = string.Empty;
}

internal sealed class AppCommentStat
{
    [JsonPropertyName("digg_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long LikeCount { get; init; }

    [JsonPropertyName("reply_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReplyCount { get; init; }
}
