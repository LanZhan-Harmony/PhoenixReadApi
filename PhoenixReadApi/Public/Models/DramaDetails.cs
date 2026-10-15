namespace PhoenixReadApi;

/// <summary>Drama metadata and its ordered episodes.</summary>
public class DramaDetails
{
    /// <summary>Drama metadata.</summary>
    public Drama Drama { get; init; } = new();

    /// <summary>Ordered episode list.</summary>
    public IReadOnlyList<Chapter> Chapters { get; init; } = [];
}

