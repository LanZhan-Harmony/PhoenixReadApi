using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Circle;

internal sealed class AppCirclePostDetailPayload
{
    [JsonPropertyName("group_ids")]
    public IReadOnlyList<string> GroupIds { get; init; } = [];

    [JsonPropertyName("group_type")]
    public int GroupType { get; init; } = 3;
}
