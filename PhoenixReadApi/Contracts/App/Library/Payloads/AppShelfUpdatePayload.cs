using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppShelfUpdatePayload
{
    [JsonPropertyName("is_active_behavior")]
    public bool IsActiveBehavior { get; init; } = true;

    [JsonPropertyName("video_shelf_operate_type")]
    public int OperationType { get; init; }

    [JsonPropertyName("update_bookshelf_video_list")]
    public IReadOnlyList<AppShelfUpdateItem> Items { get; init; } = [];
}
