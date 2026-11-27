using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppShelfBookUpdatePayload
{
    [JsonPropertyName("is_cancelled")]
    public bool IsCancelled { get; init; }

    [JsonPropertyName("update_bookshelf_video_list")]
    public IReadOnlyList<AppShelfBookUpdateItem> Items { get; init; } = [];
}

internal sealed class AppShelfBookUpdateItem
{
    [JsonPropertyName("book_id")]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("book_type")]
    public int BookType { get; init; }

    [JsonPropertyName("modify_time")]
    public long ModifyTime { get; init; }

    [JsonPropertyName("video_shelf_operate_type")]
    public int OperationType { get; init; }
}
