using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppProgressForPayload
{
    [JsonPropertyName("book_ids")]
    public IReadOnlyList<long> BookIds { get; init; } = [];
}
