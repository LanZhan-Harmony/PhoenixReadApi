using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Notification;

internal sealed class AppFollowUpdateActionPayload
{
    [JsonPropertyName("action_category")]
    public int ActionCategory { get; init; } = 104;

    [JsonPropertyName("action_type")]
    public int ActionType { get; init; } = 1;

    [JsonPropertyName("item_id")]
    public string ItemId { get; init; } = string.Empty;

    [JsonPropertyName("object_id")]
    public string ObjectId { get; init; } = string.Empty;

    [JsonPropertyName("object_type")]
    public int ObjectType { get; init; } = 1;

    [JsonPropertyName("has_aigc_content")]
    public bool HasAigcContent { get; init; }

    [JsonPropertyName("modify_count")]
    public int ModifyCount { get; init; } = 1;

    [JsonPropertyName("enter_from")]
    public string EnterFrom { get; init; } = "detail";

    [JsonPropertyName("business_param")]
    public string BusinessParam { get; init; } = "{\"enter_from\":\"detail\"}";
}
