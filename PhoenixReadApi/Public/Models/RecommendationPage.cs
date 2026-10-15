namespace PhoenixReadApi;

/// <summary>A page returned by the App recommendation endpoint.</summary>
public class RecommendationPage
{
    /// <summary>Recommendation items.</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>Next provider pagination offset.</summary>
    public int NextOffset { get; init; }

    /// <summary>Provider pagination session.</summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>Whether another page is available.</summary>
    public bool HasMore { get; init; }
}

