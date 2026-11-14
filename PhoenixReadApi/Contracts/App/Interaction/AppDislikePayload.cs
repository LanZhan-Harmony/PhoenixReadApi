using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Interaction;

internal sealed class AppDislikePayload
{
    [JsonPropertyName("action_category")]
    public int ActionCategory { get; init; } = 520;

    [JsonPropertyName("action_type")]
    public int ActionType { get; init; } = 1;

    [JsonPropertyName("object_id")]
    public string ObjectId { get; init; } = string.Empty;

    [JsonPropertyName("object_type")]
    public int ObjectType { get; init; } = 1;

    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("selection_id")]
    public int SelectionId { get; init; }

    [JsonPropertyName("action_reason")]
    public string ActionReason { get; init; } = string.Empty;

    [JsonPropertyName("recommend_info")]
    public string RecommendInfo { get; init; } = string.Empty;
}
