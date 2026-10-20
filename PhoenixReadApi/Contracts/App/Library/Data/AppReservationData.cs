using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppReservationData
{
    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppReservationItem> Items { get; init; } = [];

    [JsonPropertyName("subscribe_items")]
    public IReadOnlyList<AppReservationItem> SubscribeItems { get; init; } = [];

    [JsonPropertyName("subscribe_list")]
    public IReadOnlyList<AppReservationItem> LegacySubscribeItems { get; init; } = [];

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Total { get; init; }

    [JsonPropertyName("online_total_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long OnlineTotalCount { get; init; }

    [JsonPropertyName("offline_total_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long OfflineTotalCount { get; init; }
}
