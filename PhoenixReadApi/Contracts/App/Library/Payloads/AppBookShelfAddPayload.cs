using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppBookShelfAddPayload
{
    [JsonPropertyName("add_book_source")]
    public int AddBookSource { get; init; }

    [JsonPropertyName("identify_data")]
    public IReadOnlyList<AppBookShelfIdentifyItem> IdentifyData { get; init; } = [];
}
