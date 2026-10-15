namespace PhoenixReadApi;

/// <summary>A search suggestion.</summary>
public class SearchSuggestion
{
    /// <summary>Suggestion text.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Provider suggestion type.</summary>
    public string Type { get; init; } = string.Empty;
}

