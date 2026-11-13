namespace PhoenixReadApi;

/// <summary>评论、回复或表情包中附带的图片或表情对象。</summary>
public sealed class CommentImage
{
    /// <summary>图片或表情唯一标识符（若服务端有提供）。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>图片的主 URL。</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>图片的站内资源 URI（如 "tos-cn-i-1yzifmftcy/..."）。</summary>
    public string WebUri { get; init; } = string.Empty;

    /// <summary>备用图片 URL 列表。</summary>
    public IReadOnlyList<string> BackupUrls { get; init; } = [];

    /// <summary>图片原始宽度（像素）；若服务端未提供则为 0。</summary>
    public int Width { get; init; }

    /// <summary>图片原始高度（像素）；若服务端未提供则为 0。</summary>
    public int Height { get; init; }

    /// <summary>图片类型编码（0 为普通图片，1 为表情包等）。</summary>
    public int ImageType { get; init; }
}
