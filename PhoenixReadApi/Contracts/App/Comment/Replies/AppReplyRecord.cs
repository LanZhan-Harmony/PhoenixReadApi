using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppReplyRecord
{
    [JsonPropertyName("reply")]
    public AppReply Reply { get; init; } = new();
}
