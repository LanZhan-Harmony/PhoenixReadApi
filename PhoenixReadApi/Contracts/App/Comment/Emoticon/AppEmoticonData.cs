using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment.Emoticon;

internal sealed class AppEmoticonData
{
    [JsonPropertyName("info_list")]
    public IReadOnlyList<AppEmoticonImage> Items { get; init; } = [];

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }
}

internal sealed class AppEmoticonImage
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("web_uri")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string WebUri { get; init; } = string.Empty;

    [JsonPropertyName("web_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string WebUrl { get; init; } = string.Empty;

    [JsonPropertyName("expand_web_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ExpandWebUrl { get; init; } = string.Empty;

    [JsonPropertyName("dynamic_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string DynamicUrl { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Height { get; init; }

    [JsonPropertyName("image_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ImageType { get; init; }
}
