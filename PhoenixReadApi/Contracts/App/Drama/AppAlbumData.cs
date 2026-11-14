using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Drama;

/// <summary>合集详情接口响应数据结构。</summary>
internal sealed class AppAlbumData
{
    [JsonPropertyName("album_data")]
    [JsonConverter(typeof(FlexibleAlbumModelConverter))]
    public AppAlbumModel AlbumData { get; init; } = new();
}

internal sealed class AppAlbumModel
{
    [JsonPropertyName("album_id_str")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string AlbumIdStr { get; init; } = string.Empty;

    [JsonPropertyName("album_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string AlbumId { get; init; } = string.Empty;

    [JsonPropertyName("album_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("album_cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cover { get; init; } = string.Empty;

    [JsonPropertyName("album_intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Intro { get; init; } = string.Empty;

    [JsonPropertyName("season_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int SeasonCount { get; init; }

    [JsonPropertyName("series_list")]
    public IReadOnlyList<AppAlbumSeasonItemDto> SeriesList { get; init; } = [];

    [JsonPropertyName("video_list")]
    public IReadOnlyList<AppAlbumSeasonItemDto> VideoList { get; init; } = [];

    [JsonIgnore]
    public string EffectiveId => !string.IsNullOrWhiteSpace(AlbumIdStr) ? AlbumIdStr : AlbumId;

    [JsonIgnore]
    public IReadOnlyList<AppAlbumSeasonItemDto> EffectiveItems =>
        SeriesList.Count > 0 ? SeriesList : VideoList;
}

internal sealed class AppAlbumSeasonItemDto
{
    [JsonPropertyName("series_id_str")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesIdStr { get; init; } = string.Empty;

    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("series_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("season_index")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int SeasonIndex { get; init; }

    [JsonPropertyName("series_cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cover { get; init; } = string.Empty;

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("series_status")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int SeriesStatus { get; init; }

    [JsonIgnore]
    public string EffectiveSeriesId => !string.IsNullOrWhiteSpace(SeriesIdStr) ? SeriesIdStr : SeriesId;
}
