using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppCommentAddBusinessParameter
{
    [JsonPropertyName("book_id")]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("offset")]
    public long Offset { get; init; }

    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("ignore_urge_rule")]
    public bool IgnoreUrgeRule { get; init; }

    [JsonPropertyName("has_aigc_content")]
    public bool HasAigcContent { get; init; }

    [JsonPropertyName("shark_param")]
    public AppSharkParameter SharkParameter { get; init; } = new();
}
