using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppPostFavoriteListData
{
    [JsonPropertyName("topic_booklist")]
    public IReadOnlyList<AppTopicBooklistItem> Items { get; init; } = [];

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }
}

internal sealed class AppTopicBooklistItem
{
    [JsonPropertyName("booklist_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BooklistId { get; init; } = string.Empty;

    [JsonPropertyName("topic_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string TopicTitle { get; init; } = string.Empty;

    [JsonPropertyName("postdata")]
    public AppSearchPostDto? PostData { get; init; }
}
