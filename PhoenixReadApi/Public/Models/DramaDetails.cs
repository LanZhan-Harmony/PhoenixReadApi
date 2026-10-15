namespace PhoenixReadApi;

/// <summary>剧集信息及按顺序排列的分集列表。</summary>
public class DramaDetails
{
    /// <summary>剧集元数据。</summary>
    public Drama Drama { get; init; } = new();

    /// <summary>有序分集列表。</summary>
    public IReadOnlyList<Chapter> Chapters { get; init; } = [];
}

