using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

/// <summary>剧评和分集评论接口的请求体。</summary>
internal sealed class AppCommentListPayload
{
    [JsonPropertyName("comment_source")]
    public int CommentSource { get; init; }

    [JsonPropertyName("server_channel")]
    public int ServerChannel { get; init; }

    [JsonPropertyName("group_id")]
    public string GroupId { get; init; } = string.Empty;

    [JsonPropertyName("group_type")]
    public int GroupType { get; init; }

    [JsonPropertyName("comment_type")]
    public int CommentType { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("cursor")]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("sort")]
    public int Sort { get; init; } = 1;

    [JsonPropertyName("business_param")]
    public AppCommentListBusinessParameter BusinessParameter { get; init; } = new();
}

internal sealed class AppCommentListBusinessParameter
{
    [JsonPropertyName("book_id")]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("end_offset_time")]
    public long EndOffsetTime { get; init; }

    [JsonPropertyName("item_count")]
    public int ItemCount { get; init; }

    [JsonPropertyName("max_item_count")]
    public int MaxItemCount { get; init; }

    [JsonPropertyName("need_count")]
    public bool NeedCount { get; init; }

    [JsonPropertyName("para_index")]
    public int ParaIndex { get; init; }

    [JsonPropertyName("read_item_count")]
    public int ReadItemCount { get; init; }

    [JsonPropertyName("req_type")]
    public int RequestType { get; init; }

    [JsonPropertyName("start_offset_time")]
    public long StartOffsetTime { get; init; }

    [JsonPropertyName("playlet_item_duration")]
    public long PlayletItemDuration { get; init; }
}
