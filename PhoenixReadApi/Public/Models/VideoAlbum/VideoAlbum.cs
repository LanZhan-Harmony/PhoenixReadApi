namespace PhoenixReadApi;

/// <summary>视频合集基础元数据。</summary>
public sealed class VideoAlbum
{
    /// <summary>合集 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>合集标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>合集简介。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>合集封面图直链。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>合集收录的视频数量。</summary>
    public int VideoCount { get; init; }
}
