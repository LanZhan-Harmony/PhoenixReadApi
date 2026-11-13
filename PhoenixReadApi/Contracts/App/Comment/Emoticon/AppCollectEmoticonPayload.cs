using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment.Emoticon;

internal sealed class AppCollectEmoticonPayload
{
    [JsonPropertyName("collect_type")]
    public int CollectType { get; init; } = 1;

    [JsonPropertyName("image_id")]
    public string ImageId { get; init; } = string.Empty;

    [JsonPropertyName("image_type")]
    public int ImageType { get; init; }

    [JsonPropertyName("image_url")]
    public string ImageUrl { get; init; } = string.Empty;

    [JsonPropertyName("image_width")]
    public int ImageWidth { get; init; }

    [JsonPropertyName("image_height")]
    public int ImageHeight { get; init; }
}
