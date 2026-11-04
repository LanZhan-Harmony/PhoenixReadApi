namespace PhoenixReadApi;

/// <summary>搜索服务返回的结果。</summary>
public sealed class SearchResult
{
    /// <summary>本次搜索返回的短剧/影视列表。</summary>
    public IReadOnlyList<Drama> Dramas { get; init; } = [];

    /// <summary>本次搜索返回的原著小说列表。</summary>
    public IReadOnlyList<Novel> Novels { get; init; } = [];

    /// <summary>本次搜索返回的明星、创作者或演员用户列表。</summary>
    public IReadOnlyList<User> Users { get; init; } = [];

    /// <summary>当前搜索批次返回的结果总数。</summary>
    public int Total { get; init; }

    /// <summary>下一页请求的偏移量游标；若服务端未明确返回，则自动基于当前返回条数推算。</summary>
    public int NextOffset { get; init; }

    /// <summary>服务端是否指示存在更多搜索结果。</summary>
    public bool HasMore { get; init; }

    /// <summary>服务端本次搜索会话 ID。</summary>
    public string SearchId { get; init; } = string.Empty;

    /// <summary>结果数量是否受到客户端限制。</summary>
    public bool Limited { get; init; }

    /// <summary>搜索过程中的非致命警告。</summary>
    public string Warning { get; init; } = string.Empty;
}
