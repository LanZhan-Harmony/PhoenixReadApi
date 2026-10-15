using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

internal sealed class AppPlaybackData
{
    [JsonPropertyName("video_model")]
    [JsonConverter(typeof(VideoModelDtoConverter))]
    public AppPlaybackModel? VideoModel { get; init; }
}

internal sealed class AppPlaybackModel
{
    [JsonPropertyName("video_list")]
    [JsonConverter(typeof(FlexibleMediaVariantListConverter))]
    public IReadOnlyList<AppMediaVariantDto> Variants { get; init; } = [];

    [JsonPropertyName("video_duration")]
    [JsonConverter(typeof(FlexibleDoubleConverter))]
    public double VideoDuration { get; init; }

    [JsonPropertyName("duration")]
    [JsonConverter(typeof(FlexibleDoubleConverter))]
    public double Duration { get; init; }
}

internal sealed class AppMediaVariantDto
{
    [JsonPropertyName("video_meta")]
    public AppVideoMetaDto? Metadata { get; init; }

    [JsonPropertyName("gear_des_key")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? GearDescriptionKey { get; init; }

    [JsonPropertyName("encrypt_info")]
    public AppEncryptionDto? Encryption { get; init; }

    [JsonPropertyName("main_url")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public IReadOnlyList<string> MainUrls { get; init; } = [];

    [JsonPropertyName("backup_url")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public IReadOnlyList<string> BackupUrls { get; init; } = [];

    [JsonPropertyName("backup_url_1")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public IReadOnlyList<string> BackupUrls1 { get; init; } = [];

    [JsonPropertyName("backup_url_2")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public IReadOnlyList<string> BackupUrls2 { get; init; } = [];

    [JsonPropertyName("backup_urls")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public IReadOnlyList<string> BackupUrlCollection { get; init; } = [];

    [JsonPropertyName("url_list")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public IReadOnlyList<string> UrlList { get; init; } = [];
}

internal sealed class AppVideoMetaDto
{
    [JsonPropertyName("codec_type")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CodecType { get; init; }

    [JsonPropertyName("vheight")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Height { get; init; }

    [JsonPropertyName("vwidth")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Width { get; init; }

    [JsonPropertyName("definition")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Definition { get; init; }
}

internal sealed class AppEncryptionDto
{
    [JsonPropertyName("spade_a")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SpadeA { get; init; }

    [JsonPropertyName("encrypt")]
    public bool Encrypt { get; init; }

    [JsonPropertyName("encryption_method")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Method { get; init; }
}
