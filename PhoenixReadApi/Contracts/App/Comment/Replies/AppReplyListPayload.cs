using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppReplyListPayload
{
    [JsonPropertyName("comment_source")]
    public int CommentSource { get; init; } = 601;

    [JsonPropertyName("server_channel")]
    public int ServerChannel { get; init; } = 1000;

    [JsonPropertyName("group_id")]
    public string GroupId { get; init; } = string.Empty;

    [JsonPropertyName("comment_id")]
    public string CommentId { get; init; } = string.Empty;

    [JsonPropertyName("reply_id")]
    public string ReplyId { get; init; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; init; } = 20;

    [JsonPropertyName("cursor")]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("business_param")]
    public AppBusinessParameter BusinessParameter { get; init; } = new();
}
