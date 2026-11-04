namespace PhoenixReadApi;

/// <summary>短剧筛选面板中的一个筛选维度行（如“角色”、“时代”、“题材”）。</summary>
public sealed class ShortDramaFilterRow
{
    /// <summary>维度标识键（如 "category_dim_role"、"sort"、"creation_status"）。</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>维度显示名称（如“角色”、“排序”、“连载状态”）。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>该维度下可选的选项列表。</summary>
    public IReadOnlyList<ShortDramaFilterOption> Options { get; init; } = [];
}

/// <summary>短剧多维筛选选项条目。</summary>
public sealed class ShortDramaFilterOption
{
    /// <summary>选项显示名称（如“战神”、“甜宠”、“更新时间”）。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>选项提交值。</summary>
    public string Value { get; init; } = string.Empty;
}
