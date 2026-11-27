namespace PhoenixReadApi;

/// <summary>小说正文中的精细段落定位信息（对应服务端 <c>progress_pos_info_v2</c>）。</summary>
public sealed class ReadingPosition
{
    /// <summary>起始容器索引。</summary>
    public int StartContainerIndex { get; init; }

    /// <summary>起始元素索引。</summary>
    public int StartElementIndex { get; init; }

    /// <summary>起始元素内偏移量。</summary>
    public int StartElementOffset { get; init; }
}
