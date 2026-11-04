namespace PhoenixReadApi;

/// <summary>原著小说分类落地页信息。</summary>
public sealed class NovelCategoryPage
{
    /// <summary>分类 ID。</summary>
    public string CategoryId { get; init; } = string.Empty;

    /// <summary>分类名称。</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>该分类下的书籍列表。</summary>
    public IReadOnlyList<Novel> Books { get; init; } = [];

    /// <summary>是否还有更多书籍。</summary>
    public bool HasMore { get; init; }
}
