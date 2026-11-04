namespace PhoenixReadApi;

/// <summary>媒体视频编码格式。</summary>
public enum MediaCodec
{
    /// <summary>未知编码格式。</summary>
    Unknown = 0,

    /// <summary>AVC / H.264 编码（兼容性最佳）。</summary>
    H264 = 1,

    /// <summary>ByteVC1 / H.265 / HEVC 编码（压缩率高）。</summary>
    ByteVC1 = 2,

    /// <summary>ByteVC2 / H.266 / VVC 编码。</summary>
    ByteVC2 = 3,
}
