using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppBookShelfIdentifyItem
{
    [JsonPropertyName("asterisked")]
    public bool Asterisked { get; init; }

    [JsonPropertyName("book_id")]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("book_type")]
    public int BookType { get; init; }

    [JsonPropertyName("modify_time")]
    public long ModifyTime { get; init; }
}
