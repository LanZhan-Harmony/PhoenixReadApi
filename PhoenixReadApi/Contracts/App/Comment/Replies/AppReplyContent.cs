using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppReplyContent
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}
