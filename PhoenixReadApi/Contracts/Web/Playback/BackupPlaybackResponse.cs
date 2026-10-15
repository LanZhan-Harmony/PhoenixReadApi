using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Contracts.Web.Playback;

internal sealed class BackupPlaybackResponseDto
{
    [JsonPropertyName("parse")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Parse { get; init; }

    [JsonPropertyName("jx")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Jx { get; init; }

    [JsonPropertyName("key_urls")]
    public IReadOnlyList<BackupMediaOptionDto> KeyUrls { get; init; } = [];
}

internal sealed class BackupMediaOptionDto
{
    [JsonPropertyName("src")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Source { get; init; }

    [JsonPropertyName("kid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? KeyId { get; init; }

    [JsonPropertyName("spade_a")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SpadeA { get; init; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Name { get; init; }
}
