namespace PhoenixReadApi;

/// <summary>Search response returned by the Phoenix index.</summary>
public class SearchResult
{
    /// <summary>Search items.</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>Total reported by the provider.</summary>
    public int Total { get; init; }

    /// <summary>Whether the result was bounded by the client.</summary>
    public bool Limited { get; init; }

    /// <summary>Non-fatal search warning.</summary>
    public string Warning { get; init; } = string.Empty;
}

