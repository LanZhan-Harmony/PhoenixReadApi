using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppReadProgressUploadItem
{
    [JsonPropertyName("book_id")]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("book_type")]
    public int BookType { get; init; }

    [JsonPropertyName("channel_id")]
    public long ChannelId { get; init; }

    [JsonPropertyName("check_timestamp")]
    public bool CheckTimestamp { get; init; }

    [JsonPropertyName("cur_channel_id")]
    public long CurrentChannelId { get; init; }

    [JsonPropertyName("current_play_time")]
    public long CurrentPlayTime { get; init; }

    [JsonPropertyName("genre_type")]
    public int GenreType { get; init; }

    [JsonPropertyName("is_listen_mode")]
    public bool IsListenMode { get; init; }

    [JsonPropertyName("is_local_book")]
    public bool IsLocalBook { get; init; }

    [JsonPropertyName("item_id")]
    public string ItemId { get; init; } = string.Empty;

    [JsonPropertyName("item_progress_rate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ItemProgressRate { get; init; }

    [JsonPropertyName("listen_and_read")]
    public bool ListenAndRead { get; init; }

    [JsonPropertyName("page_index")]
    public int PageIndex { get; init; }

    [JsonPropertyName("page_progress_rate")]
    public float PageProgressRate { get; init; }

    [JsonPropertyName("paragraph_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParagraphId { get; init; }

    [JsonPropertyName("paragraph_offset")]
    public int ParagraphOffset { get; init; }

    [JsonPropertyName("player_cumulative_total_duration")]
    public long PlayerCumulativeTotalDuration { get; init; }

    [JsonPropertyName("progress_pos_info_v2")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AppReadProgressPosition? ProgressPosInfoV2 { get; init; }

    [JsonPropertyName("progress_rate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProgressRate { get; init; }

    [JsonPropertyName("progress_type")]
    public int ProgressType { get; init; }

    [JsonPropertyName("read_timestamp_ms")]
    public long ReadTimestampMs { get; init; }

    [JsonPropertyName("tone_id")]
    public int ToneId { get; init; }

    [JsonPropertyName("vid_index")]
    public int VideoIndex { get; init; }
}
