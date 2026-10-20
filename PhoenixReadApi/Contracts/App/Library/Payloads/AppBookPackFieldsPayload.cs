using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppBookPackFieldsPayload
{
    [JsonPropertyName("book_pack_channel")]
    public string Channel { get; init; } = string.Empty;
}
