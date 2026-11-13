using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppUploadPictureData
{
    [JsonPropertyName("web_uri")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string WebUri { get; init; } = string.Empty;

    [JsonPropertyName("web_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string WebUrl { get; init; } = string.Empty;

    [JsonPropertyName("image_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ImageId { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Height { get; init; }
}
