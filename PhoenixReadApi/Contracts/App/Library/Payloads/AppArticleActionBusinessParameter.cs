using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Comment;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppArticleActionBusinessParameter
{
    [JsonPropertyName("book_id")]
    public long BookId { get; init; }

    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("has_aigc_content")]
    public bool HasAigcContent { get; init; }

    [JsonPropertyName("modify_count")]
    public int ModifyCount { get; init; }

    [JsonPropertyName("screen_frame")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ScreenFrame { get; init; } = string.Empty;

    [JsonPropertyName("shark_param")]
    public AppSharkParameter SharkParameter { get; init; } = new();
}
