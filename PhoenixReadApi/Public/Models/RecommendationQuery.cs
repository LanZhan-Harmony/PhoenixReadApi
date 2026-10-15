namespace PhoenixReadApi;

/// <summary>Input cursor for the App recommendation endpoint.</summary>
public class RecommendationQuery
{
    /// <summary>Recommendation genre.</summary>
    public string Genre { get; init; } = string.Empty;

    /// <summary>Provider pagination offset.</summary>
    public int Offset { get; init; }

    /// <summary>Provider pagination session.</summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>Previously seen provider identifiers.</summary>
    public IReadOnlyList<string> Seen { get; init; } = [];
}

