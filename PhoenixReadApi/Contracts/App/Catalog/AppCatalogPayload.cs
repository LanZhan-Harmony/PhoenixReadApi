using System.Text.Json.Serialization;

namespace PhoenixReadApi.Contracts.App.Catalog;

internal sealed class AppCatalogPayload
{
    [JsonPropertyName("req_scene")]
    public string RequestScene { get; init; } = string.Empty;

    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 18;

    [JsonPropertyName("req_type")]
    public string RequestType { get; init; } = "only_content";

    [JsonPropertyName("need_selector_panel")]
    public bool NeedSelectorPanel { get; init; }

    [JsonPropertyName("client_req_type")]
    public int ClientRequestType { get; init; } = 3;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("filter_ids")]
    public string FilterIds { get; init; } = string.Empty;

    [JsonPropertyName("select_items")]
    public AppCatalogSelectItems SelectItems { get; init; } = new();
}
