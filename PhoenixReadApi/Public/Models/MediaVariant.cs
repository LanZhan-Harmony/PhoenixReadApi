namespace PhoenixReadApi;

/// <summary>A media variant sorted by provider quality.</summary>
public class MediaVariant
{
    /// <summary>Playable media URL.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Quality score parsed from the provider label.</summary>
    public int Quality { get; init; }

    /// <summary>Optional CENC decryption key.</summary>
    public string DecryptionKey { get; init; } = string.Empty;
}

