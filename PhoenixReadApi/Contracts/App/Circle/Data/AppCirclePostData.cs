using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Circle;

internal sealed class AppCirclePostData
{
    [JsonPropertyName("post_data")]
    public IReadOnlyList<AppCirclePostItem> Posts { get; init; } = [];
}
