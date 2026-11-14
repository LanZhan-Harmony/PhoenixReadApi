namespace PhoenixReadApi;

/// <summary>原著小说分类落地页信息。</summary>
public sealed class NovelCategoryPage
{
    /// <summary>分类分类数字标识 ID。</summary>
    public string CategoryId { get; init; } = string.Empty;

    /// <summary>分类显示名称（如“都市修真”、“战神归来”）。</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>本页小说列表。</summary>
    public IReadOnlyList<Novel> Items { get; init; } = [];

    /// <summary>是否还有更多小说。</summary>
    public bool HasMore { get; init; }
}
