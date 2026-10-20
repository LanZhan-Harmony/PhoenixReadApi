using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppFollowActionPayload
{
    [JsonPropertyName("object_id")]
    public string ObjectId { get; init; } = string.Empty;

    [JsonPropertyName("object_type")]
    public int ObjectType { get; init; } = 1;

    [JsonPropertyName("action_type")]
    public int ActionType { get; init; }

    [JsonPropertyName("aid")]
    public int Aid { get; init; } = 8662;

    [JsonPropertyName("business_param")]
    public AppFollowBusinessParameter BusinessParameter { get; init; } = new();
}
