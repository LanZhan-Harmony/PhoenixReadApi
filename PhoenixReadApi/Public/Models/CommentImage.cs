namespace PhoenixReadApi;

/// <summary>评论或回复中附带的图片。</summary>
public sealed class CommentImage
{
    /// <summary>图片的主 URL。</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>备用图片 URL 列表。</summary>
    public IReadOnlyList<string> BackupUrls { get; init; } = [];

    /// <summary>图片原始宽度（像素）；若服务端未提供则为 0。</summary>
    public int Width { get; init; }

    /// <summary>图片原始高度（像素）；若服务端未提供则为 0。</summary>
    public int Height { get; init; }
}
