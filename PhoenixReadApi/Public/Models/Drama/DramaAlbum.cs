namespace PhoenixReadApi;

/// <summary>短剧多季合集信息及其包含的各个季。</summary>
public sealed class DramaAlbum
{
    /// <summary>合集 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>合集标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>合集简介。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>合集封面图直链。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>季数总数。</summary>
    public int SeasonCount { get; init; }

    /// <summary>合集收录的各季列表，通常按季序号升序排列。</summary>
    public IReadOnlyList<DramaSeason> Seasons { get; init; } = [];
}
