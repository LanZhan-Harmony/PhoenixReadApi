using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

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

    [JsonPropertyName("url_expire")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long UrlExpire { get; init; }

    [JsonPropertyName("dynamic_video")]
    public AppDynamicVideoDto DynamicVideo { get; init; } = new();
}

internal sealed class AppDynamicVideoDto
{
    [JsonPropertyName("dynamic_type")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string DynamicType { get; init; } = string.Empty;

    [JsonPropertyName("dynamic_video_list")]
    [JsonConverter(typeof(FlexibleMediaVariantListConverter))]
    public IReadOnlyList<AppMediaVariantDto> DynamicVideoList { get; init; } = [];

    [JsonPropertyName("dynamic_audio_list")]
    public IReadOnlyList<AppDynamicAudioDto> DynamicAudioList { get; init; } = [];
}

internal sealed class AppDynamicAudioDto
{
    [JsonPropertyName("main_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string MainUrl { get; init; } = string.Empty;

    [JsonPropertyName("encrypt_info")]
    public AppEncryptionDto Encryption { get; init; } = new();
}

internal sealed class AppMediaVariantDto
{
    [JsonPropertyName("video_meta")]
    public AppVideoMetaDto Metadata { get; init; } = new();

    [JsonPropertyName("gear_des_key")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string GearDescriptionKey { get; init; } = string.Empty;

    [JsonPropertyName("encrypt_info")]
    public AppEncryptionDto Encryption { get; init; } = new();

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

    [JsonPropertyName("definition")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string DirectDefinition { get; init; } = string.Empty;

    [JsonPropertyName("codec_type")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string DirectCodecType { get; init; } = string.Empty;

    [JsonPropertyName("vheight")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int DirectHeight { get; init; }

    [JsonPropertyName("vwidth")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int DirectWidth { get; init; }

    [JsonPropertyName("spade_a")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string DirectSpadeA { get; init; } = string.Empty;

    [JsonPropertyName("encrypt")]
    public bool DirectEncrypt { get; init; }

    [JsonPropertyName("encryption_method")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string DirectEncryptionMethod { get; init; } = string.Empty;

    [JsonIgnore]
    public string EffectiveDefinition =>
        !string.IsNullOrWhiteSpace(Metadata.Definition) ? Metadata.Definition : DirectDefinition;

    [JsonIgnore]
    public string EffectiveCodecType =>
        !string.IsNullOrWhiteSpace(Metadata.CodecType) ? Metadata.CodecType : DirectCodecType;

    [JsonIgnore]
    public int EffectiveHeight => Metadata.Height > 0 ? Metadata.Height : DirectHeight;

    [JsonIgnore]
    public int EffectiveWidth => Metadata.Width > 0 ? Metadata.Width : DirectWidth;

    [JsonIgnore]
    public string EffectiveSpadeA =>
        !string.IsNullOrWhiteSpace(Encryption.SpadeA) ? Encryption.SpadeA : DirectSpadeA;

    [JsonIgnore]
    public bool EffectiveEncrypt => Encryption.Encrypt || DirectEncrypt;

    [JsonIgnore]
    public string EffectiveEncryptionMethod =>
        !string.IsNullOrWhiteSpace(Encryption.Method) ? Encryption.Method : DirectEncryptionMethod;

    [JsonIgnore]
    public string EffectiveQualityName =>
        !string.IsNullOrWhiteSpace(Metadata.Quality) ? Metadata.Quality : GearDescriptionKey;

    [JsonIgnore]
    public long EffectiveBitrate => Metadata.Bitrate;

    [JsonIgnore]
    public long EffectiveSize => Metadata.Size;

    [JsonIgnore]
    public string EffectiveBackupUrl =>
        BackupUrls.Count > 0 ? BackupUrls[0] :
        BackupUrlCollection.Count > 0 ? BackupUrlCollection[0] :
        BackupUrls1.Count > 0 ? BackupUrls1[0] :
        BackupUrls2.Count > 0 ? BackupUrls2[0] : string.Empty;

    [JsonIgnore]
    public bool EffectiveIsHls =>
        GearDescriptionKey.Contains("HLS", StringComparison.OrdinalIgnoreCase)
        || Metadata.VideoType.Contains("hls", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool EffectiveIsDash =>
        GearDescriptionKey.Contains("DASH", StringComparison.OrdinalIgnoreCase)
        || Metadata.VideoType.Contains("dash", StringComparison.OrdinalIgnoreCase);
}

internal sealed class AppVideoMetaDto
{
    [JsonPropertyName("codec_type")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CodecType { get; init; } = string.Empty;

    [JsonPropertyName("vheight")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Height { get; init; }

    [JsonPropertyName("vwidth")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Width { get; init; }

    [JsonPropertyName("definition")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Definition { get; init; } = string.Empty;

    [JsonPropertyName("quality")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Quality { get; init; } = string.Empty;

    [JsonPropertyName("bitrate")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Bitrate { get; init; }

    [JsonPropertyName("size")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Size { get; init; }

    [JsonPropertyName("vtype")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoType { get; init; } = string.Empty;
}

internal sealed class AppEncryptionDto
{
    [JsonPropertyName("spade_a")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SpadeA { get; init; } = string.Empty;

    [JsonPropertyName("encrypt")]
    public bool Encrypt { get; init; }

    [JsonPropertyName("encryption_method")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Method { get; init; } = string.Empty;
}
