using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppReadHistoryUpdateItem
{
    [JsonPropertyName("book_id")]
    public long BookId { get; init; }

    [JsonPropertyName("book_type")]
    public int BookType { get; init; } = 2;

    [JsonPropertyName("chapter_index")]
    public int ChapterIndex { get; init; }

    [JsonPropertyName("current_play_position")]
    public long CurrentPlayPosition { get; init; }

    [JsonPropertyName("digged_count")]
    public int DiggedCount { get; init; }

    [JsonPropertyName("duration")]
    public long Duration { get; init; }

    [JsonPropertyName("episode_cnt")]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("is_delete")]
    public bool IsDelete { get; init; }

    [JsonPropertyName("is_interactive_game")]
    public bool IsInteractiveGame { get; init; }

    [JsonPropertyName("is_listen_mode")]
    public bool IsListenMode { get; init; }

    [JsonPropertyName("is_multi_season")]
    public int IsMultiSeason { get; init; }

    [JsonPropertyName("meet_guide_comment_tag")]
    public bool MeetGuideCommentTag { get; init; }

    [JsonPropertyName("origin_novel_book_id")]
    public long OriginNovelBookId { get; init; }

    [JsonPropertyName("player_accumulate_total_time")]
    public long PlayerAccumulateTotalTime { get; init; }

    [JsonPropertyName("read_timestamp_ms")]
    public long ReadTimestampMs { get; init; }

    [JsonPropertyName("recent_reads")]
    public int RecentReads { get; init; }

    [JsonPropertyName("retain_video_play_time")]
    public int RetainVideoPlayTime { get; init; }

    [JsonPropertyName("season_index")]
    public int SeasonIndex { get; init; }

    [JsonPropertyName("series_play_cnt")]
    public int SeriesPlayCount { get; init; }

    [JsonPropertyName("tone_id")]
    public int ToneId { get; init; }

    [JsonPropertyName("update_timestamp_ms")]
    public long UpdateTimestampMs { get; init; }

    [JsonPropertyName("use_soft_delete")]
    public bool UseSoftDelete { get; init; }

    [JsonPropertyName("user_digg")]
    public bool UserDigg { get; init; }

    [JsonPropertyName("user_playlet_comment_flag")]
    public bool UserPlayletCommentFlag { get; init; }

    [JsonPropertyName("vid")]
    public long VideoId { get; init; }

    [JsonPropertyName("vid_index")]
    public int VideoIndex { get; init; } = 1;
}
