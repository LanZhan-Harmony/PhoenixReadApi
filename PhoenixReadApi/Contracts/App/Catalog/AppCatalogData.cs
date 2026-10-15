using System.Text.Json;
using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Contracts.App.Catalog;

internal sealed class AppCatalogData
{
    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<AppDramaDto> Items { get; init; } = [];

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SessionId { get; init; }
}

/// <summary>剧库、详情和推荐响应共用的红果剧集字段。</summary>
internal sealed class AppDramaDto
{
    [JsonPropertyName("video_data")]
    public AppDramaDto? VideoData { get; init; }

    [JsonPropertyName("series_id_str")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SeriesId { get; init; }

    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SeriesIdNumber { get; init; }

    [JsonPropertyName("keyword")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Keyword { get; init; }

    [JsonPropertyName("series_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SeriesTitle { get; init; }

    [JsonPropertyName("series_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SeriesName { get; init; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Name { get; init; }

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Title { get; init; }

    [JsonPropertyName("series_intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SeriesIntro { get; init; }

    [JsonPropertyName("video_desc")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? VideoDescription { get; init; }

    [JsonPropertyName("series_cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SeriesCover { get; init; }

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Cover { get; init; }

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("episode_right_text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? EpisodeRemark { get; init; }

    [JsonPropertyName("category_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CategoryName { get; init; }

    [JsonPropertyName("categoryName")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CategoryNameCamel { get; init; }

    [JsonPropertyName("category")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Category { get; init; }

    [JsonPropertyName("series_status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SeriesStatus { get; init; }

    [JsonPropertyName("score")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Score { get; init; }

    [JsonPropertyName("series_play_cnt")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SeriesPlayCount { get; init; }

    [JsonPropertyName("play_cnt")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? PlayCount { get; init; }

    [JsonPropertyName("hot_score")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? HotScore { get; init; }

    [JsonPropertyName("hot_score_data")]
    public AppHotScoreData? HotScoreData { get; init; }

    [JsonPropertyName("first_visible_time")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? FirstVisibleTime { get; init; }

    [JsonPropertyName("tags")]
    public JsonElement Tags { get; init; }

    [JsonPropertyName("category_list")]
    public IReadOnlyList<AppCategoryDto> CategoryList { get; init; } = [];

    [JsonPropertyName("category_schema")]
    [JsonConverter(typeof(CategorySchemaConverter))]
    public List<string> CategorySchema { get; init; } = [];

    [JsonPropertyName("sub_title_list")]
    public IReadOnlyList<AppSubtitleDto> Subtitles { get; init; } = [];
}

internal sealed class AppHotScoreData
{
    [JsonPropertyName("score")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Score { get; init; }

    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Text { get; init; }
}

internal sealed class AppCategoryDto
{
    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Name { get; init; }
}

internal sealed class AppSubtitleDto
{
    [JsonPropertyName("content")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Content { get; init; }
}
