namespace PhoenixReadApi;

/// <summary>A playable media address and its optional CENC key.</summary>
public class PlaybackMedia
{
    /// <summary>Playable media URL.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Referer required by the provider.</summary>
    public string Referer { get; init; } = string.Empty;

    /// <summary>Media duration.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Quality score parsed from the provider label.</summary>
    public int Quality { get; init; }

    /// <summary>Optional CENC decryption key.</summary>
    public string DecryptionKey { get; init; } = string.Empty;

    /// <summary>Available media variants.</summary>
    public IReadOnlyList<MediaVariant> Variants { get; init; } = [];
}

