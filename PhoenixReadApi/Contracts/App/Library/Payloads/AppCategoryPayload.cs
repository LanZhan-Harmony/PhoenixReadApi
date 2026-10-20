using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

internal sealed class AppCategoryPayload
{
    [JsonPropertyName("need_selector_panel")]
    public bool NeedSelectorPanel { get; init; } = true;

    [JsonPropertyName("client_req_type")]
    public int ClientRequestType { get; init; } = 3;

    [JsonPropertyName("req_scene")]
    public string RequestScene { get; init; } = "default";

    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 18;

    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("select_items")]
    public AppCategorySelectedItems SelectedItems { get; init; } = new();
}

internal sealed class AppCategorySelectedItems
{
    [JsonPropertyName("genre")]
    public IReadOnlyList<string> Genre { get; init; } = [];

    [JsonPropertyName("category_theme")]
    public IReadOnlyList<string> CategoryTheme { get; init; } = [];

    [JsonPropertyName("category_role")]
    public IReadOnlyList<string> CategoryRole { get; init; } = [];

    [JsonPropertyName("category_epoch")]
    public IReadOnlyList<string> CategoryEpoch { get; init; } = [];

    [JsonPropertyName("online_time")]
    public IReadOnlyList<string> OnlineTime { get; init; } = [];

    [JsonPropertyName("creation_status")]
    public IReadOnlyList<string> CreationStatus { get; init; } = [];
}
