using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppReply
{
    [JsonPropertyName("reply_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("comment_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CommentId { get; init; } = string.Empty;

    [JsonPropertyName("reply_to_user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ReplyToUserId { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public AppReplyContent Content { get; init; } = new();

    [JsonPropertyName("user_info")]
    public AppReplyUser User { get; init; } = new();

    [JsonPropertyName("create_timestamp")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CreatedAt { get; init; }

    [JsonPropertyName("digg_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long LikeCount { get; init; }
}
