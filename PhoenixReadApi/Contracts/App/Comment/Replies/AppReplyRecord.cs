using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppReplyRecord
{
    [JsonPropertyName("reply_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ReplyId { get; init; } = string.Empty;

    [JsonPropertyName("reply_to_comment_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ReplyToCommentId { get; init; } = string.Empty;

    [JsonPropertyName("reply_to_user_info")]
    public AppReplyUserInfo ReplyToUserInfo { get; init; } = new();

    [JsonPropertyName("stat")]
    public AppReplyStat Stat { get; init; } = new();

    [JsonPropertyName("Common")]
    public AppReplyCommon Common { get; init; } = new();

    [JsonPropertyName("reply")]
    public AppReply Reply { get; init; } = new();
}

internal sealed class AppReplyCommon
{
    [JsonPropertyName("content")]
    public AppReplyContent Content { get; init; } = new();

    [JsonPropertyName("create_timestamp")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CreateTimestamp { get; init; }

    [JsonPropertyName("user_info")]
    public AppReplyUserInfo UserInfo { get; init; } = new();
}

internal sealed class AppReplyUserInfo
{
    [JsonPropertyName("base_info")]
    public AppReplyUserBaseInfo BaseInfo { get; init; } = new();
}

internal sealed class AppReplyUserBaseInfo
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("user_avatar")]
    public string UserAvatar { get; init; } = string.Empty;
}

internal sealed class AppReplyStat
{
    [JsonPropertyName("digg_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long DiggCount { get; init; }

    [JsonPropertyName("reply_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReplyCount { get; init; }
}
