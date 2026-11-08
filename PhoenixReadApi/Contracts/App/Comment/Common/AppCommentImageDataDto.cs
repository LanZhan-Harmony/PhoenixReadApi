using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

[JsonConverter(typeof(FlexibleCommentImageDataListConverter))]
internal sealed class AppCommentImageDataListDto
{
    [JsonPropertyName("image_data")]
    public IReadOnlyList<AppCommentImageDataDto> ImageData { get; init; } = [];
}

internal sealed class AppCommentImageDataDto
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("web_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string WebUrl { get; init; } = string.Empty;

    [JsonPropertyName("expand_web_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ExpandWebUrl { get; init; } = string.Empty;

    [JsonPropertyName("web_uri")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string WebUri { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Height { get; init; }

    [JsonPropertyName("format")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Format { get; init; } = string.Empty;

    [JsonPropertyName("image_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ImageType { get; init; }
}
