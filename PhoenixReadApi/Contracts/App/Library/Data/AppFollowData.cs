using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppFollowData
{
    [JsonPropertyName("mix_data")]
    public IReadOnlyList<AppFollowItem> Items { get; init; } = [];

    [JsonPropertyName("comment_list_info")]
    public AppFollowListInfo ListInfo { get; init; } = new();
}
