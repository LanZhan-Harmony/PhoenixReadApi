using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppCommentAddData
{
    [JsonPropertyName("comment_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CommentId { get; init; } = string.Empty;

    [JsonPropertyName("comment_info")]
    public AppCommentAddCommentInfo CommentInfo { get; init; } = new();
}

internal sealed class AppCommentAddCommentInfo
{
    [JsonPropertyName("comment_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CommentId { get; init; } = string.Empty;
}
