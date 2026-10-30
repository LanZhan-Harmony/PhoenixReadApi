using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppReplyAddData
{
    [JsonPropertyName("reply_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ReplyId { get; init; } = string.Empty;

    [JsonPropertyName("reply")]
    public AppReplyAddReplyInfo Reply { get; init; } = new();
}

internal sealed class AppReplyAddReplyInfo
{
    [JsonPropertyName("reply_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ReplyId { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;
}
