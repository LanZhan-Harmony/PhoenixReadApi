using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppReadProgressUploadPayload
{
    [JsonPropertyName("books")]
    public IReadOnlyList<AppReadProgressUploadItem> Books { get; init; } = [];
}
