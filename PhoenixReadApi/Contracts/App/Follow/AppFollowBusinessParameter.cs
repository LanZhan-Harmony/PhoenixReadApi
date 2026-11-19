using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Comment;

namespace PhoenixReadApi.Internal.Contracts.App.Follow;

internal sealed class AppFollowBusinessParameter
{
    [JsonPropertyName("shark_param")]
    public AppSharkParameter SharkParameter { get; init; } = new();
}
