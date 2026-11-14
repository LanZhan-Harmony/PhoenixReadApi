namespace PhoenixReadApi;

/// <summary>多季合集中的单季剧集元数据。</summary>
public sealed class DramaSeason
{
    /// <summary>该季对应的短剧 ID，可传给 GetDramaDetailsAsync。</summary>
    public string DramaId { get; init; } = string.Empty;

    /// <summary>该季短剧标题（例如“xxx 第一季”）。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>季序号（从 1 开始）。</summary>
    public int Index { get; init; }

    /// <summary>该季短剧封面图直链。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>该季分集总集数。</summary>
    public int EpisodeCount { get; init; }

    /// <summary>连载状态。</summary>
    public DramaReleaseStatus Status { get; init; } = DramaReleaseStatus.Unknown;
}
