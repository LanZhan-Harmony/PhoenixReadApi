namespace PhoenixReadApi;

/// <summary>题材分类标签分组（如热门、情节、主题、背景、角色等）。</summary>
public sealed record TagCategoryGroup
{
    /// <summary>分组名称（如“热门”、“情节”等）。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>该分组下的标签项列表。</summary>
    public IReadOnlyList<TagCategoryItem> Items { get; init; } = [];
}

/// <summary>单个题材分类标签项。</summary>
public sealed record TagCategoryItem
{
    /// <summary>标签标识（如“cate_261”）。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>标签展示名称（如“都市日常”）。</summary>
    public string Name { get; init; } = string.Empty;
}
