using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppCommentPayload
{
    [JsonPropertyName("comment_source")]
    public int CommentSource { get; init; } = 601;

    [JsonPropertyName("server_channel")]
    public int ServerChannel { get; init; } = 1000;

    [JsonPropertyName("group_id")]
    public string GroupId { get; init; } = string.Empty;

    [JsonPropertyName("group_type")]
    public int GroupType { get; init; } = 30;

    [JsonPropertyName("comment_type")]
    public int CommentType { get; init; } = 20;

    [JsonPropertyName("sort")]
    public int Sort { get; init; } = 1;

    [JsonPropertyName("count")]
    public int Count { get; init; } = 90;

    [JsonPropertyName("cursor")]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("business_param")]
    public AppCommentBusinessParameter BusinessParameter { get; init; } = new();
}
