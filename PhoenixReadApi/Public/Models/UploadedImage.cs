namespace PhoenixReadApi;

/// <summary>图片上传成功后返回的图片元数据。</summary>
public sealed class UploadedImage
{
    /// <summary>图片在服务端的唯一标识符。</summary>
    public string ImageId { get; init; } = string.Empty;

    /// <summary>图片公网访问 URL。</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>图片的站内资源 URI（用于发布评论与剧评）。</summary>
    public string WebUri { get; init; } = string.Empty;

    /// <summary>图片宽度（像素）。</summary>
    public int Width { get; init; }

    /// <summary>图片高度（像素）。</summary>
    public int Height { get; init; }
}
