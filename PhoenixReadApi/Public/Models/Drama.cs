using System.Text.Json.Serialization;

namespace PhoenixReadApi;

/// <summary>A drama returned by the catalog or search endpoints.</summary>
public class Drama
{
    /// <summary>Provider-independent identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Stable provider source name.</summary>
    public string Source { get; init; } = "hongguo";

    /// <summary>Identifier assigned by the provider.</summary>
    public string SourceId { get; init; } = string.Empty;

    /// <summary>Drama title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Drama name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Drama description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Cover image URL.</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>Provider category.</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Display channel.</summary>
    public string Channel { get; init; } = "红果";

    /// <summary>Provider remark.</summary>
    public string Remark { get; init; } = string.Empty;

    /// <summary>Provider score.</summary>
    public string Score { get; init; } = string.Empty;

    /// <summary>Provider view count.</summary>
    public string Views { get; init; } = string.Empty;

    /// <summary>Provider heat value.</summary>
    public string Heat { get; init; } = string.Empty;

    /// <summary>First visible date.</summary>
    public string OnlineDate { get; init; } = string.Empty;

    /// <summary>Number of episodes.</summary>
    public int EpisodeCount { get; init; }

    /// <summary>Release status.</summary>
    public string ReleaseStatus { get; init; } = string.Empty;

    /// <summary>Provider tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Returns the best available display title.</summary>
    // Keep this computed convenience property out of JSON if an application chooses to
    // serialize a public model with its own context.
    [JsonIgnore]
    public string DisplayTitle =>
        string.IsNullOrWhiteSpace(Title)
            ? (string.IsNullOrWhiteSpace(Name) ? "短剧" : Name)
            : Title;
}

