namespace PhoenixReadApi;

/// <summary>搜索服务返回的结果。</summary>
public sealed class SearchResult
{
    /// <summary>搜索结果条目。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>搜索结果总数。</summary>
    public int Total { get; init; }

    /// <summary>结果数量是否受到客户端限制。</summary>
    public bool Limited { get; init; }

    /// <summary>搜索过程中的非致命警告。</summary>
    public string Warning { get; init; } = string.Empty;
}

