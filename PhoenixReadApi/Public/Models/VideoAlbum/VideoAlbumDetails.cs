namespace PhoenixReadApi;

/// <summary>视频合集详情及收录的短视频列表。</summary>
public sealed class VideoAlbumDetails
{
    /// <summary>合集基础元数据。</summary>
    public VideoAlbum Album { get; init; } = new();

    /// <summary>合集收录的短视频列表（每个条目均可直接用于播放、评论与弹幕操作）。</summary>
    public IReadOnlyList<ShortVideo> Videos { get; init; } = [];
}
