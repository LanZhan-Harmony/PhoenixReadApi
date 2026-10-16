using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Danmaku;

internal sealed class AppDanmakuData
{
    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppCommentRecord> Items { get; init; } = [];

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
