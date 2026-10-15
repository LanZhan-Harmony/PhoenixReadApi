namespace PhoenixReadApi;

/// <summary>包含兜底诊断信息的剧库结果。</summary>
public class CatalogResult
{
    /// <summary>剧库结果条目。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>本次请求的剧库分类。</summary>
    public PhoenixReadApiCategory Category { get; init; }

    /// <summary>是否使用了网页剧库接口兜底。</summary>
    public bool UsedWebFallback { get; init; }

    /// <summary>非致命诊断信息。</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

