namespace PhoenixReadApi;

/// <summary>剧集信息及按顺序排列的分集列表与扩展元数据。</summary>
public sealed class DramaDetails
{
    /// <summary>剧集元数据。</summary>
    public Drama Drama { get; init; } = new();

    /// <summary>按集数排序的分集列表。</summary>
    public IReadOnlyList<Episode> Episodes { get; init; } = [];

    /// <summary>关联的多季合集 ID（若该短剧属于多季作品）。</summary>
    public string RelatedAlbumId { get; init; } = string.Empty;

    /// <summary>该剧是否为多季短剧之一。</summary>
    public bool IsMultiSeason { get; init; }

    /// <summary>当前剧集在多季合集中的季序号（从 1 开始）。</summary>
    public int SeasonIndex { get; init; } = 1;

    /// <summary>该短剧改编的原著小说信息（若服务端有提供）。</summary>
    public Novel? SourceNovel { get; init; }
}
