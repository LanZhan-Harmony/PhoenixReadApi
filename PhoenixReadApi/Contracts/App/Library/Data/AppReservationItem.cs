using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppReservationItem
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("book_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("item_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ItemId { get; init; } = string.Empty;

    [JsonPropertyName("video_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("item_desc")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("create_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReservedAt { get; init; }

    [JsonPropertyName("publish_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReleaseAt { get; init; }

    [JsonPropertyName("expected_publish_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ExpectedPublishAt { get; init; }

    [JsonPropertyName("is_online")]
    public bool IsOnline { get; init; }
}
