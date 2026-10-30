using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Shared.Drama;

/// <summary>App、网页和搜索联想共用的剧集记录。</summary>
internal sealed class DramaDto
{
    [JsonPropertyName("series_id_str")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesIdNumber { get; init; } = string.Empty;

    [JsonPropertyName("book_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("keyword")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Keyword { get; init; } = string.Empty;

    [JsonPropertyName("series_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesTitle { get; init; } = string.Empty;

    [JsonPropertyName("series_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesName { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("series_intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesIntro { get; init; } = string.Empty;

    [JsonPropertyName("video_desc")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoDescription { get; init; } = string.Empty;

    [JsonPropertyName("abstract")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Abstract { get; init; } = string.Empty;

    [JsonPropertyName("intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Intro { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("series_cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesCover { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cover { get; init; } = string.Empty;

    [JsonPropertyName("cover_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("thumb_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ThumbUrl { get; init; } = string.Empty;

    [JsonPropertyName("expand_thumb_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ExpandThumbUrl { get; init; } = string.Empty;

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("episode_count")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCountAlias { get; init; }

    [JsonPropertyName("drama_chapter_number")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int DramaChapterNumber { get; init; }

    [JsonPropertyName("chapter_number")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ChapterNumber { get; init; }

    [JsonPropertyName("episode_right_text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string EpisodeRemark { get; init; } = string.Empty;

    [JsonPropertyName("category_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("categoryName")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CategoryNameCamel { get; init; } = string.Empty;

    [JsonPropertyName("category")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("series_status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesStatus { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("score")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Score { get; init; } = string.Empty;

    [JsonPropertyName("final_score_v2")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string FinalScoreV2 { get; init; } = string.Empty;

    [JsonPropertyName("final_score")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string FinalScore { get; init; } = string.Empty;

    [JsonPropertyName("author")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Author { get; init; } = string.Empty;

    [JsonPropertyName("series_play_cnt")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesPlayCount { get; init; } = string.Empty;

    [JsonPropertyName("play_cnt")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string PlayCount { get; init; } = string.Empty;

    [JsonPropertyName("hot_score")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string HotScore { get; init; } = string.Empty;

    [JsonPropertyName("hot_score_data")]
    public DramaHotScoreDto HotScoreData { get; init; } = new();

    [JsonPropertyName("first_visible_time")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string FirstVisibleTime { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    [JsonConverter(typeof(DramaTagListConverter))]
    public IReadOnlyList<DramaTagDto> Tags { get; init; } = [];

    [JsonPropertyName("category_list")]
    public IReadOnlyList<DramaCategoryDto> CategoryList { get; init; } = [];

    [JsonPropertyName("category_schema")]
    [JsonConverter(typeof(CategorySchemaConverter))]
    public IReadOnlyList<string> CategorySchema { get; init; } = [];

    [JsonPropertyName("sub_title_list")]
    public IReadOnlyList<DramaSubtitleDto> Subtitles { get; init; } = [];

    [JsonPropertyName("vid_list")]
    [JsonConverter(typeof(FlexibleStringListConverter))]
    public IReadOnlyList<string> VideoIds { get; init; } = [];

    [JsonPropertyName("vid")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("pay_info")]
    public DramaPayInfoDto PayInfo { get; init; } = new();

    [JsonPropertyName("video_list")]
    [JsonConverter(typeof(FlexibleVideoListConverter))]
    public IReadOnlyList<DramaEpisodeDto> VideoList { get; init; } = [];

    [JsonPropertyName("celebrities")]
    public IReadOnlyList<DramaCelebrityDto> Celebrities { get; init; } = [];

    [JsonPropertyName("main_creator_users")]
    public IReadOnlyList<DramaCreatorUserDto> MainCreatorUsers { get; init; } = [];
}
