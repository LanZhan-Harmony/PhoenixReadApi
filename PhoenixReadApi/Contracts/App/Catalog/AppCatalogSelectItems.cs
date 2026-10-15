using System.Text.Json.Serialization;

namespace PhoenixReadApi.Contracts.App.Catalog;

internal sealed class AppCatalogSelectItems
{
    [JsonPropertyName("genre")]
    public IReadOnlyList<string> Genre { get; init; } = [];

    [JsonPropertyName("sort")]
    public IReadOnlyList<string> Sort { get; init; } = [];

    [JsonPropertyName("gender")]
    public IReadOnlyList<string> Gender { get; init; } = [];

    [JsonPropertyName("category_dim_theme")]
    public IReadOnlyList<string> CategoryTheme { get; init; } = [];

    [JsonPropertyName("category_dim_role")]
    public IReadOnlyList<string> CategoryRole { get; init; } = [];

    [JsonPropertyName("category_dim_epoch")]
    public IReadOnlyList<string> CategoryEpoch { get; init; } = [];

    [JsonPropertyName("online_time")]
    public IReadOnlyList<string> OnlineTime { get; init; } = [];

    [JsonPropertyName("creation_status")]
    public IReadOnlyList<string> CreationStatus { get; init; } = [];
}
