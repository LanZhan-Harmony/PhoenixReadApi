namespace PhoenixReadApi;

/// <summary>单个题材分类标签项。</summary>
public sealed class TagCategoryItem
{
    /// <summary>标签标识（如“cate_261”）。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>标签展示名称（如“都市日常”）。</summary>
    public string Name { get; init; } = string.Empty;
}
