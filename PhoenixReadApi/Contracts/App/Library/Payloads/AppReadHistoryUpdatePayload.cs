using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppReadHistoryUpdatePayload
{
    [JsonPropertyName("update_datas")]
    public IReadOnlyList<AppReadHistoryUpdateItem> Items { get; init; } = [];
}
