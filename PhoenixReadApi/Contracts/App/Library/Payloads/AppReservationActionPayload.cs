using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Comment;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppReservationActionPayload
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; init; } = string.Empty;

    [JsonPropertyName("item_type")]
    public int ItemType { get; init; } = 14;

    [JsonPropertyName("op_type")]
    public int OperationType { get; init; }

    [JsonPropertyName("wish_list_all_del")]
    public int WishListAllDelete { get; init; }

    [JsonPropertyName("not_del_item_id")]
    public string NotDeleteItemId { get; init; } = string.Empty;

    [JsonPropertyName("shark_param")]
    public AppSharkParameter SharkParameter { get; init; } = new();
}
