using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppReplyAddData
{
    [JsonPropertyName("reply_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ReplyId { get; init; } = string.Empty;

    [JsonPropertyName("reply")]
    public AppReplyRecord Reply { get; init; } = new();
}
