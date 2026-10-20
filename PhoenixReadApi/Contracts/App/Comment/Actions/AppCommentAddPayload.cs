using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

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

    [JsonPropertyName("reply_to_user_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ReplyToUserId { get; init; } = string.Empty;

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
