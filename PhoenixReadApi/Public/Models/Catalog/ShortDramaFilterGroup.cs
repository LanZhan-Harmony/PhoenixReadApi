namespace PhoenixReadApi;

/// <summary>短剧筛选面板中的一个筛选分组（如“角色”、“时代”、“题材”）。</summary>
public sealed class ShortDramaFilterGroup
{
    /// <summary>筛选维度标识（例如 "category", "era", "tag"）。</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>筛选维度在界面上的展示名称（例如“题材”、“时代”）。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>该筛选维度下的可选选项列表。</summary>
    public IReadOnlyList<ShortDramaFilterItem> Items { get; init; } = [];
}
