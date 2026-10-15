using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppCommentBusinessParameter
{
    [JsonPropertyName("book_id")]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("start_offset_time")]
    public long StartOffsetTime { get; init; }

    [JsonPropertyName("playlet_item_duration")]
    public long PlayletItemDuration { get; init; }

    [JsonPropertyName("need_danmaku_guide_type")]
    public IReadOnlyList<int> NeedDanmakuGuideType { get; init; } = [1, 3, 4, 2];
}
