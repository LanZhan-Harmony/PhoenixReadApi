namespace PhoenixReadApi;

/// <summary>红果圈图文图片信息。</summary>
public sealed class CircleImage
{
    /// <summary>图片 URL。</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>图片宽度（像素）。</summary>
    public int Width { get; init; }

    /// <summary>图片高度（像素）。</summary>
    public int Height { get; init; }
}
