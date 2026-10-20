using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppCommentActionPayload
{
    [JsonPropertyName("object_id")]
    public string ObjectId { get; init; } = string.Empty;

    [JsonPropertyName("object_type")]
    public int ObjectType { get; init; } = 2;

    [JsonPropertyName("action_type")]
    public int ActionType { get; init; }

    [JsonPropertyName("comment_type")]
    public int CommentType { get; init; } = 4;

    [JsonPropertyName("aid")]
    public int Aid { get; init; } = 8662;

    [JsonPropertyName("business_param")]
    public AppCommentActionBusinessParameter BusinessParameter { get; init; } = new();
}
