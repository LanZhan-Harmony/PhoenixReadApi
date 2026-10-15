namespace PhoenixReadApi;

/// <summary>Catalog response with fallback diagnostics.</summary>
public class CatalogResult
{
    /// <summary>Catalog items.</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>Requested catalog category.</summary>
    public PhoenixReadApiCategory Category { get; init; }

    /// <summary>Whether the web catalog fallback was used.</summary>
    public bool UsedWebFallback { get; init; }

    /// <summary>Non-fatal diagnostics.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

