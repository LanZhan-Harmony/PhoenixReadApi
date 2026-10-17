using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;

namespace PhoenixReadApi.Internal.Contracts.App.Details;

internal sealed class AppDetailsData
{
    [JsonPropertyName("video_data")]
    public DramaDto VideoData { get; init; } = new();
}
