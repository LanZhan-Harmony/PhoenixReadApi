using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppReplyUser
{
    [JsonPropertyName("user_id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("user_avatar")]
    public string AvatarUrl { get; init; } = string.Empty;
}
