using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppBookShelfListData
{
    [JsonPropertyName("book_shelf_info_all")]
    public IReadOnlyList<AppBookShelfItem> Items { get; init; } = [];

    [JsonPropertyName("limit")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Limit { get; init; }
}

internal sealed class AppBookShelfItem
{
    [JsonPropertyName("book_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("book_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int BookType { get; init; }

    [JsonPropertyName("serial_count")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int SerialCount { get; init; }

    [JsonPropertyName("creation_status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CreationStatus { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Tags { get; init; } = string.Empty;
}
