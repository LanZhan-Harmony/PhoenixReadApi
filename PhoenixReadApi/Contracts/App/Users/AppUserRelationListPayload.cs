using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Users;

internal sealed class AppUserRelationListPayload
{
    [JsonPropertyName("count")]
    public int Count { get; init; } = 20;

    [JsonPropertyName("comment_source")]
    public int CommentSource { get; init; }

    [JsonPropertyName("target_user_id")]
    public string TargetUserId { get; init; } = string.Empty;

    [JsonPropertyName("cursor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Cursor { get; init; }
}
