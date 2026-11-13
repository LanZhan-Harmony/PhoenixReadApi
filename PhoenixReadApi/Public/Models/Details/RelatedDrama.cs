namespace PhoenixReadApi;

/// <summary>一个剧集关联的分季或相关推荐记录。</summary>
public sealed class RelatedDrama
{
    /// <summary>剧集信息。</summary>
    public Drama Drama { get; init; } = new();

    /// <summary>在关联列表中的顺序。</summary>
    public int Order { get; init; }

    /// <summary>关联关系说明。</summary>
    public string Relation { get; init; } = string.Empty;
}
