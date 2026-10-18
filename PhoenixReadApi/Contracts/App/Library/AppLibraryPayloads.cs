using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppProgressForPayload
{
    [JsonPropertyName("book_ids")]
    public IReadOnlyList<long> BookIds { get; init; } = [];
}

internal sealed class AppBookPackFieldsPayload
{
    [JsonPropertyName("book_pack_channel")]
    public string Channel { get; init; } = string.Empty;
}

internal sealed class AppProgressUpdatePayload
{
    [JsonPropertyName("book_type")]
    public int BookType { get; init; } = 2;

    [JsonPropertyName("vid")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("book_id")]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("chapter_index")]
    public int ChapterIndex { get; init; }

    [JsonPropertyName("vid_index")]
    public int VideoIndex { get; init; } = 1;

    [JsonPropertyName("is_delete")]
    public bool IsDelete { get; init; }

    [JsonPropertyName("current_play_position")]
    public long CurrentPlayPositionMs { get; init; }

    [JsonPropertyName("digged_count")]
    public int DiggedCount { get; init; }

    [JsonPropertyName("duration")]
    public long DurationMs { get; init; }

    [JsonPropertyName("episode_cnt")]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("current_play_time")]
    public long CurrentPlayTimeMs { get; init; }

    [JsonPropertyName("player_accumulate_total_time")]
    public long PlayerAccumulateTotalTimeMs { get; init; }

    [JsonPropertyName("player_cumulative_total_duration")]
    public long PlayerCumulativeTotalDurationMs { get; init; }

    [JsonPropertyName("read_timestamp_ms")]
    public long ReadTimestampMs { get; init; }

    [JsonPropertyName("recent_reads")]
    public int RecentReads { get; init; }

    [JsonPropertyName("season_index")]
    public int SeasonIndex { get; init; }

    [JsonPropertyName("tone_id")]
    public int ToneId { get; init; }

    [JsonPropertyName("user_digg")]
    public bool UserDigg { get; init; }

    [JsonPropertyName("update_timestamp_ms")]
    public long UpdateTimestampMs { get; init; }

    [JsonPropertyName("use_soft_delete")]
    public bool UseSoftDelete { get; init; }

    [JsonPropertyName("is_interactive_game")]
    public bool IsInteractiveGame { get; init; }

    [JsonPropertyName("is_listen_mode")]
    public bool IsListenMode { get; init; }

    [JsonPropertyName("is_multi_season")]
    public int IsMultiSeason { get; init; }

    [JsonPropertyName("origin_novel_book_id")]
    public long OriginNovelBookId { get; init; }

    [JsonPropertyName("retain_video_play_time")]
    public int RetainVideoPlayTime { get; init; }

    [JsonPropertyName("series_play_cnt")]
    public int SeriesPlayCount { get; init; }

    [JsonPropertyName("meet_guide_comment_tag")]
    public bool MeetGuideCommentTag { get; init; }

    [JsonPropertyName("user_playlet_comment_flag")]
    public bool UserPlayletCommentFlag { get; init; }

    [JsonPropertyName("cur_channel_id")]
    public string CurrentChannelId { get; init; } = string.Empty;

    [JsonPropertyName("check_timestamp")]
    public long CheckTimestamp { get; init; }

    [JsonPropertyName("listen_and_read")]
    public bool ListenAndRead { get; init; }

    [JsonPropertyName("page_progress_rate")]
    public double PageProgressRate { get; init; }

    [JsonPropertyName("paragraph_offset")]
    public int ParagraphOffset { get; init; }
}

internal sealed class AppShelfUpdatePayload
{
    [JsonPropertyName("is_active_behavior")]
    public bool IsActiveBehavior { get; init; } = true;

    [JsonPropertyName("video_shelf_operate_type")]
    public int OperationType { get; init; }

    [JsonPropertyName("update_bookshelf_video_list")]
    public IReadOnlyList<AppShelfUpdateItem> Items { get; init; } = [];
}

internal sealed class AppShelfUpdateItem
{
    [JsonPropertyName("series_id")]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("vid")]
    public string VideoId { get; init; } = string.Empty;
}

internal sealed class AppReservationActionPayload
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; init; } = string.Empty;

    [JsonPropertyName("item_type")]
    public int ItemType { get; init; } = 14;

    [JsonPropertyName("op_type")]
    public int OperationType { get; init; }

    [JsonPropertyName("wish_list_all_del")]
    public int WishListAllDelete { get; init; }

    [JsonPropertyName("not_del_item_id")]
    public string NotDeleteItemId { get; init; } = string.Empty;

    [JsonPropertyName("shark_param")]
    public AppSharkParameter SharkParameter { get; init; } = new();
}

internal sealed class AppFollowActionPayload
{
    [JsonPropertyName("action_type")]
    public int ActionType { get; init; }

    [JsonPropertyName("user_id")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("business_param")]
    public AppBusinessParameter BusinessParameter { get; init; } = new();
}

internal sealed class AppArticleActionPayload
{
    [JsonPropertyName("object_id")]
    public string ObjectId { get; init; } = string.Empty;

    [JsonPropertyName("object_type")]
    public int ObjectType { get; init; } = 6;

    [JsonPropertyName("action_type")]
    public int ActionType { get; init; }

    [JsonPropertyName("aid")]
    public int Aid { get; init; } = 8662;

    [JsonPropertyName("action_category")]
    public int ActionCategory { get; init; }

    [JsonPropertyName("action_reason_remark")]
    public string ActionReasonRemark { get; init; } = "like_click";

    [JsonPropertyName("has_aigc_content")]
    public bool HasAigcContent { get; init; }

    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("business_param")]
    public AppArticleActionBusinessParameter BusinessParameter { get; init; } = new();
}

internal sealed class AppArticleActionBusinessParameter
{
    [JsonPropertyName("book_id")]
    public long BookId { get; init; }

    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("has_aigc_content")]
    public bool HasAigcContent { get; init; }

    [JsonPropertyName("modify_count")]
    public int ModifyCount { get; init; }

    [JsonPropertyName("screen_frame")]
    public string ScreenFrame { get; init; } = string.Empty;

    [JsonPropertyName("shark_param")]
    public AppSharkParameter SharkParameter { get; init; } = new();
}

internal sealed class AppCommentActionPayload
{
    [JsonPropertyName("object_id")]
    public string ObjectId { get; init; } = string.Empty;

    [JsonPropertyName("object_type")]
    public int ObjectType { get; init; } = 2;

    [JsonPropertyName("action_type")]
    public int ActionType { get; init; }

    [JsonPropertyName("comment_type")]
    public int CommentType { get; init; } = 4;

    [JsonPropertyName("aid")]
    public int Aid { get; init; } = 8662;

    [JsonPropertyName("business_param")]
    public AppCommentActionBusinessParameter BusinessParameter { get; init; } = new();
}

internal sealed class AppBusinessParameter
{
    [JsonPropertyName("book_id")]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;
}

internal sealed class AppCommentActionBusinessParameter
{
    [JsonPropertyName("shark_param")]
    public AppSharkParameter SharkParameter { get; init; } = new();

    [JsonPropertyName("book_id")]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;
}

internal sealed class AppSharkParameter
{
    [JsonPropertyName("enter_from")]
    public string EnterFrom { get; init; } = "ShortSeriesActivity";

    [JsonPropertyName("previous_page")]
    public string PreviousPage { get; init; } = "MainFragmentActivity";

    [JsonPropertyName("page_list")]
    public string PageList { get; init; } = "MainFragmentActivity,ShortSeriesActivity";
}

internal sealed class AppCommentAddPayload
{
    [JsonPropertyName("commit_source")]
    public int CommitSource { get; init; }

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("group_id")]
    public string GroupId { get; init; } = string.Empty;

    [JsonPropertyName("group_type")]
    public int GroupType { get; init; }

    [JsonPropertyName("data_type")]
    public int DataType { get; init; }

    [JsonPropertyName("comment_type")]
    public int CommentType { get; init; }

    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("aid")]
    public int Aid { get; init; } = 8662;

    [JsonPropertyName("reply_to_comment_id")]
    public string ReplyToCommentId { get; init; } = string.Empty;

    [JsonPropertyName("ignore_urge_rule")]
    public bool IgnoreUrgeRule { get; init; }

    [JsonPropertyName("has_aigc_content")]
    public bool HasAigcContent { get; init; }

    [JsonPropertyName("preset_text_id")]
    public string PresetTextId { get; init; } = string.Empty;

    [JsonPropertyName("add_one_parent_id")]
    public string ParentId { get; init; } = string.Empty;

    [JsonPropertyName("business_param")]
    public AppCommentAddBusinessParameter BusinessParameter { get; init; } = new();
}

internal sealed class AppCommentAddBusinessParameter
{
    [JsonPropertyName("book_id")]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("video_id")]
    public string VideoId { get; init; } = string.Empty;

    [JsonPropertyName("offset")]
    public long Offset { get; init; }

    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("ignore_urge_rule")]
    public bool IgnoreUrgeRule { get; init; }

    [JsonPropertyName("has_aigc_content")]
    public bool HasAigcContent { get; init; }

    [JsonPropertyName("shark_param")]
    public AppSharkParameter SharkParameter { get; init; } = new();
}

internal sealed class AppCommentDeletePayload
{
    [JsonPropertyName("comment_id")]
    public string CommentId { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public int DataType { get; init; } = 4;

    [JsonPropertyName("service_id")]
    public int ServiceId { get; init; } = 4;

    [JsonPropertyName("aid")]
    public int Aid { get; init; } = 8662;

    [JsonPropertyName("business_param")]
    public AppBusinessParameter BusinessParameter { get; init; } = new();
}

internal sealed class AppReplyListPayload
{
    [JsonPropertyName("comment_source")]
    public int CommentSource { get; init; } = 601;

    [JsonPropertyName("server_channel")]
    public int ServerChannel { get; init; } = 1000;

    [JsonPropertyName("group_id")]
    public string GroupId { get; init; } = string.Empty;

    [JsonPropertyName("comment_id")]
    public string CommentId { get; init; } = string.Empty;

    [JsonPropertyName("reply_id")]
    public string ReplyId { get; init; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; init; } = 20;

    [JsonPropertyName("cursor")]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("business_param")]
    public AppBusinessParameter BusinessParameter { get; init; } = new();
}

internal sealed class AppReplyListData
{
    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppReplyRecord> Items { get; init; } = [];

    [JsonPropertyName("reply_list")]
    public IReadOnlyList<AppReplyRecord> ReplyItems { get; init; } = [];

    [JsonPropertyName("common_list_info")]
    public AppReplyListInfo ListInfo { get; init; } = new();

    [JsonPropertyName("comment_list_info")]
    public AppReplyListInfo CommentListInfo { get; init; } = new();
}

internal sealed class AppReplyListInfo
{
    [JsonPropertyName("cursor")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Total { get; init; }
}

internal sealed class AppReplyRecord
{
    [JsonPropertyName("reply")]
    public AppReply Reply { get; init; } = new();
}

internal sealed class AppReply
{
    [JsonPropertyName("reply_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("comment_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CommentId { get; init; } = string.Empty;

    [JsonPropertyName("reply_to_user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ReplyToUserId { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public AppReplyContent Content { get; init; } = new();

    [JsonPropertyName("user_info")]
    public AppReplyUser User { get; init; } = new();

    [JsonPropertyName("create_timestamp")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CreatedAt { get; init; }

    [JsonPropertyName("digg_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long LikeCount { get; init; }
}

internal sealed class AppReplyContent
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}

internal sealed class AppReplyUser
{
    [JsonPropertyName("user_id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("user_avatar")]
    public string AvatarUrl { get; init; } = string.Empty;
}
