using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppShelfListData
{
    [JsonPropertyName("video_shelf_info")]
    public IReadOnlyList<AppShelfItem> ShelfItems { get; init; } = [];

    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppShelfItem> Items { get; init; } = [];

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Total { get; init; }
}
