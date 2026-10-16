using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppActorProfilePayload
{
    [JsonPropertyName("comment_source")]
    public int CommentSource { get; init; } = 1506;

    [JsonPropertyName("count")]
    public int Count { get; init; } = 18;

    [JsonPropertyName("cursor")]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("locate_post_id")]
    public string LocatePostId { get; init; } = string.Empty;

    [JsonPropertyName("profile_user_type")]
    public int ProfileUserType { get; init; }

    [JsonPropertyName("sort")]
    public string Sort { get; init; } = "time_desc";

    [JsonPropertyName("tab_type")]
    public int TabType { get; init; } = 8;

    [JsonPropertyName("target_user_id")]
    public string TargetUserId { get; init; } = string.Empty;
}
