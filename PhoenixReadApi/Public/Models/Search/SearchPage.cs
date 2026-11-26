namespace PhoenixReadApi;

/// <summary>泛型搜索分页结果。</summary>
/// <typeparam name="T">当前搜索页包含的领域实体或联合类型。</typeparam>
public sealed class SearchPage<T>
{
    /// <summary>当前页的结果条目集合。</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>服务端返回的总数或当前累计参考总数。</summary>
    public int Total { get; init; }

    /// <summary>下一页请求的偏移量游标。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有更多搜索结果。</summary>
    public bool HasMore { get; init; }

    /// <summary>服务端本次搜索会话 ID，翻页时应回传。</summary>
    public string SearchId { get; init; } = string.Empty;

    /// <summary>服务端透传翻页上下文。</summary>
    public string Passback { get; init; } = string.Empty;

    /// <summary>结果数量是否受到客户端限制。</summary>
    public bool Limited { get; init; }

    /// <summary>搜索过程中的非致命警告信息。</summary>
    public string Warning { get; init; } = string.Empty;
}
