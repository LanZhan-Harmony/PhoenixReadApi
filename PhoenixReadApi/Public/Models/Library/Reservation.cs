namespace PhoenixReadApi;

/// <summary>一条预约记录。</summary>
public sealed class Reservation
{
    /// <summary>预约记录 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>剧集 ID。</summary>
    public string SeriesId { get; init; } = string.Empty;

    /// <summary>视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>剧集标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>封面 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>预约时间（Unix 秒）。</summary>
    public long ReservedAtUnixSeconds { get; init; }

    /// <summary>预计更新时间（Unix 秒）。</summary>
    public long ReleaseAtUnixSeconds { get; init; }

    /// <summary>预约是否仍然有效。</summary>
    public bool IsActive { get; init; }
}
