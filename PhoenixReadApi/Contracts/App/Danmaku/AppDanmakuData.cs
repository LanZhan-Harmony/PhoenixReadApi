using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Danmaku;

internal sealed class AppDanmakuData
{
    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppDanmakuRecord> Items { get; init; } = [];

    [JsonPropertyName("extra")]
    public AppDanmakuExtra Extra { get; init; } = new();

    [JsonPropertyName("common_list_info")]
    public AppCommentListInfo ListInfo { get; init; } = new();
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
    public string Cursor { get; init; } = string.Empty;
}

internal sealed class AppDanmakuRecord
{
    [JsonPropertyName("comment")]
    public AppComment Comment { get; init; } = new();
}

internal sealed class AppComment
{
    [JsonPropertyName("common")]
    public AppCommentCommon Common { get; init; } = new();

    [JsonPropertyName("expand")]
    public AppCommentExpand Expand { get; init; } = new();

    [JsonPropertyName("comment_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;
}

internal sealed class AppCommentCommon
{
    [JsonPropertyName("group_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string GroupId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public AppCommentContent Content { get; init; } = new();
}

internal sealed class AppCommentContent
{
    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Text { get; init; } = string.Empty;
}

internal sealed class AppCommentExpand
{
    [JsonPropertyName("offset_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long OffsetTime { get; init; }
}
