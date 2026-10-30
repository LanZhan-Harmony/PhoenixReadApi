using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppReplyListPayload
{
    [JsonPropertyName("comment_source")]
    public int CommentSource { get; init; } = 501;

    [JsonPropertyName("server_channel")]
    public int ServerChannel { get; init; } = 34;

    [JsonPropertyName("group_id")]
    public string GroupId { get; init; } = string.Empty;

    [JsonPropertyName("group_type")]
    public int GroupType { get; init; }

    [JsonPropertyName("comment_type")]
    public int CommentType { get; init; }

    [JsonPropertyName("comment_id")]
    public string CommentId { get; init; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; init; } = 20;

    [JsonPropertyName("cursor")]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("aid")]
    public int Aid { get; init; } = 8662;

    [JsonPropertyName("compliance_status")]
    public int ComplianceStatus { get; init; }

    [JsonPropertyName("business_param")]
    public AppReplyBusinessParameter BusinessParameter { get; init; } = new();
}

internal sealed class AppReplyBusinessParameter
{
    [JsonPropertyName("book_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("need_count")]
    public bool NeedCount { get; init; }

    [JsonPropertyName("real_level")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int RealLevel { get; init; }
}
