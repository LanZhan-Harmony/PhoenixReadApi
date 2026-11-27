using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppBookShelfDeletePayload
{
    [JsonPropertyName("identify_data")]
    public IReadOnlyList<AppBookShelfIdentifyItem> IdentifyData { get; init; } = [];
}
