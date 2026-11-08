using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppCommentImageDto
{
    [JsonPropertyName("uri")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Uri { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("url_list")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public IReadOnlyList<string> UrlList { get; init; } = [];

    [JsonPropertyName("download_url")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public IReadOnlyList<string> DownloadUrl { get; init; } = [];

    [JsonPropertyName("download_url_list")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public IReadOnlyList<string> DownloadUrlList { get; init; } = [];

    [JsonPropertyName("width")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Height { get; init; }
}
