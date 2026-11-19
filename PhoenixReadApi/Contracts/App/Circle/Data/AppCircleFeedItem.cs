using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Circle;

internal sealed class AppCircleFeedItem
{
    [JsonPropertyName("data_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int DataType { get; init; }

    [JsonPropertyName("video")]
    public AppCircleVideoItem? Video { get; init; }

    [JsonPropertyName("post")]
    public AppCirclePostItem? Post { get; init; }
}
