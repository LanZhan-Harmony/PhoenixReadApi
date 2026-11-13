namespace PhoenixReadApi;

/// <summary>短剧多维筛选选项条目。</summary>
public sealed class ShortDramaFilterOption
{
    /// <summary>选项显示名称（如“战神”、“甜宠”、“更新时间”）。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>选项提交值。</summary>
    public string Value { get; init; } = string.Empty;
}
