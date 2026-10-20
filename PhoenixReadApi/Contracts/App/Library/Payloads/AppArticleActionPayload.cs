using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppArticleActionPayload
{
    [JsonPropertyName("object_id")]
    public string ObjectId { get; init; } = string.Empty;

    [JsonPropertyName("object_type")]
    public int ObjectType { get; init; } = 6;

    [JsonPropertyName("action_type")]
    public int ActionType { get; init; }

    [JsonPropertyName("action_category")]
    public int ActionCategory { get; init; }

    [JsonPropertyName("action_reason_remark")]
    public string ActionReasonRemark { get; init; } = "like_click";

    [JsonPropertyName("business_param")]
    public AppArticleActionBusinessParameter BusinessParameter { get; init; } = new();
}
