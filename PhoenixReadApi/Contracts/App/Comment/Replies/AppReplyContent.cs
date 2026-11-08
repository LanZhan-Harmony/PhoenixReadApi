using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppReplyContent
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("image_data_list")]
    public AppCommentImageDataListDto ImageDataList { get; init; } = new();

    [JsonPropertyName("image_list")]
    public IReadOnlyList<AppCommentImageDto> ImageList { get; init; } = [];
}
