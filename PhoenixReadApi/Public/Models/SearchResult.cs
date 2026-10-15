namespace PhoenixReadApi;

/// <summary>红果搜索索引返回的结果。</summary>
public class SearchResult
{
    /// <summary>搜索结果条目。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>红果接口返回的总数。</summary>
    public int Total { get; init; }

    /// <summary>结果数量是否受到客户端限制。</summary>
    public bool Limited { get; init; }

    /// <summary>搜索过程中的非致命警告。</summary>
    public string Warning { get; init; } = string.Empty;
}

