using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppReadProgressPosition
{
    [JsonPropertyName("start_container_index")]
    public int StartContainerIndex { get; init; }

    [JsonPropertyName("start_element_index")]
    public int StartElementIndex { get; init; }

    [JsonPropertyName("start_element_offset")]
    public int StartElementOffset { get; init; }
}
