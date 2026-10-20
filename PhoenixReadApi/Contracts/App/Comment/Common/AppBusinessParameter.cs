using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppBusinessParameter
{
    [JsonPropertyName("book_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("video_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string VideoId { get; init; } = string.Empty;
}
