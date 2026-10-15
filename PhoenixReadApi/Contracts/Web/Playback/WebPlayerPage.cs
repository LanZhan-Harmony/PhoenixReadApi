using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Contracts.Web.Playback;

internal sealed class WebPlayerPageDto
{
    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? VideoId { get; init; }

    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SeriesId { get; init; }

    [JsonPropertyName("video_player_info")]
    public WebPlayerInfoDto? PlayerInfo { get; init; }
}

internal sealed class WebPlayerInfoDto
{
    [JsonPropertyName("duration")]
    [JsonConverter(typeof(FlexibleDoubleConverter))]
    public double Duration { get; init; }

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
