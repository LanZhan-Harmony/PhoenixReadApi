using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Danmaku;

internal sealed class AppDanmakuData
{
    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppDanmakuRecord> Items { get; init; } = [];

    [JsonPropertyName("extra")]
    public AppDanmakuExtra? Extra { get; init; }

    [JsonPropertyName("common_list_info")]
    public AppCommentListInfo? ListInfo { get; init; }
}

internal sealed class AppDanmakuExtra
{
    [JsonPropertyName("next_query_danmaku_list_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long NextTime { get; init; }
}

internal sealed class AppCommentListInfo
{
    [JsonPropertyName("cursor")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Cursor { get; init; }
}

internal sealed class AppDanmakuRecord
{
    [JsonPropertyName("comment")]
    public AppComment? Comment { get; init; }
}

internal sealed class AppComment
{
    [JsonPropertyName("common")]
    public AppCommentCommon? Common { get; init; }

    [JsonPropertyName("expand")]
    public AppCommentExpand? Expand { get; init; }

    [JsonPropertyName("comment_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Id { get; init; }
}

internal sealed class AppCommentCommon
{
    [JsonPropertyName("group_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? GroupId { get; init; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Status { get; init; }

    [JsonPropertyName("content")]
    public AppCommentContent? Content { get; init; }
}

internal sealed class AppCommentContent
{
    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Text { get; init; }
}

internal sealed class AppCommentExpand
{
    [JsonPropertyName("offset_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long OffsetTime { get; init; }
}
