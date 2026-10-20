using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

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
