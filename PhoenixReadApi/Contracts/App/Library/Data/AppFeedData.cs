using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppCellChangeData
{
    [JsonPropertyName("cell_view")]
    public IReadOnlyList<AppFeedCell> Cells { get; init; } = [];

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;
}

internal sealed class AppFeedTabData
{
    [JsonPropertyName("tab_item")]
    public IReadOnlyList<AppFeedTab> Tabs { get; init; } = [];
}

internal sealed class AppFeedTab
{
    [JsonPropertyName("tab_type")]
    public int Type { get; init; }

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("cell_data")]
    public IReadOnlyList<AppFeedCell> Cells { get; init; } = [];
}

internal sealed class AppFeedCell
{
    [JsonPropertyName("cell_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("cell_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> VideoItems { get; init; } = [];

    [JsonPropertyName("book_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> BookItems { get; init; } = [];

    [JsonPropertyName("cell_data")]
    public IReadOnlyList<AppFeedCell> Children { get; init; } = [];
}
