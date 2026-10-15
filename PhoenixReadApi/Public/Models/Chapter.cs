namespace PhoenixReadApi;

/// <summary>An episode belonging to a drama.</summary>
public class Chapter
{
    /// <summary>Provider-independent episode identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Stable provider source name.</summary>
    public string Source { get; init; } = "hongguo";

    /// <summary>Episode title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Provider video identifier or URL.</summary>
    public string VideoUrl { get; init; } = string.Empty;

    /// <summary>One-based episode number.</summary>
    public int Episode { get; init; }

    /// <summary>Web page URL.</summary>
    public string PageUrl { get; init; } = string.Empty;

    /// <summary>Referer required by the provider.</summary>
    public string Referer { get; init; } = string.Empty;
}

