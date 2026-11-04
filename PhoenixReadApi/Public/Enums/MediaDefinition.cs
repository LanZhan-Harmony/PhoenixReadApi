namespace PhoenixReadApi;

/// <summary>媒体清晰度级别（对应垂直高度像素分辨率）。</summary>
public enum MediaDefinition
{
    /// <summary>未知或未明确指定清晰度。</summary>
    Unknown = 0,

    /// <summary>标清 360p 流。</summary>
    P360 = 360,

    /// <summary>标清 480p 流。</summary>
    P480 = 480,

    /// <summary>高清 540p 流。</summary>
    P540 = 540,

    /// <summary>高清 720p 流。</summary>
    P720 = 720,

    /// <summary>超清 1080p 流。</summary>
    P1080 = 1080,

    /// <summary>4K 2160p 流。</summary>
    P2160 = 2160,
}
