using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppCommentDeletePayload
{
    [JsonPropertyName("comment_id")]
    public string CommentId { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public int DataType { get; init; } = 4;

    [JsonPropertyName("service_id")]
    public int ServiceId { get; init; } = 4;

    [JsonPropertyName("aid")]
    public int Aid { get; init; } = 8662;
}
