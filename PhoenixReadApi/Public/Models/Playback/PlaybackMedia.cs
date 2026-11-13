namespace PhoenixReadApi;

/// <summary>可播放的媒体地址及其规格参数、解密密钥与播放要求。</summary>
/// <remarks>
/// 解析结果通常按清晰度综合得分从高到低排序，列表中首个条目即为最佳推荐画质。
/// 若包含 CENC 密钥，需传给 FFmpeg 的 -decryption_key 参数或播放内核。
/// </remarks>
public sealed class PlaybackMedia
{
    /// <summary>可播放媒体 URL 直链。</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>备用 CDN 播放直链。</summary>
    public string BackupUrl { get; init; } = string.Empty;

    /// <summary>播放器要求的 Referer 请求头。</summary>
    public string Referer { get; init; } = string.Empty;

    /// <summary>媒体时长。</summary>
    public TimeSpan Duration { get; init; } = TimeSpan.Zero;

    /// <summary>清晰度枚举（如 P720, P1080）。</summary>
    public MediaDefinition Definition { get; init; } = MediaDefinition.Unknown;

    /// <summary>画质级别名称（如 "normal", "high", "超清"）。</summary>
    public string QualityName { get; init; } = string.Empty;

    /// <summary>视频编码格式枚举（如 H264, ByteVC1）。</summary>
    public MediaCodec Codec { get; init; } = MediaCodec.Unknown;

    /// <summary>视频宽度像素。</summary>
    public int Width { get; init; }

    /// <summary>视频高度像素。</summary>
    public int Height { get; init; }

    /// <summary>码率（单位：bit/s）。</summary>
    public long Bitrate { get; init; }

    /// <summary>文件大小（单位：字节）。</summary>
    public long Size { get; init; }

    /// <summary>从媒体标签解析出的清晰度综合分数。</summary>
    public int Quality { get; init; }

    /// <summary>CENC AES-128 内容解密密钥（16 字节十六进制小写字符串）。</summary>
    public string DecryptionKey { get; init; } = string.Empty;

    /// <summary>独立音频轨地址（长片电影或分段 DASH 媒体）。</summary>
    public string AudioUrl { get; init; } = string.Empty;

    /// <summary>流直链防盗链过期时间戳（Unix 秒）。</summary>
    public long ExpiresAtUnixSeconds { get; init; }

    /// <summary>是否为 HLS (m3u8) 播放列表。</summary>
    public bool IsHls { get; init; }

    /// <summary>是否为 DASH 分段流。</summary>
    public bool IsDash { get; init; }
}
