using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Comment;

namespace PhoenixReadApi.Internal.Contracts.App.Circle;

internal sealed class AppCircleFeedData
{
    [JsonPropertyName("common_list_info")]
    public AppUserListInfo ListInfo { get; init; } = new();

    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppCircleFeedItem> Items { get; init; } = [];

    [JsonPropertyName("tabs")]
    public IReadOnlyList<AppUserProfileTab> Tabs { get; init; } = [];
}
