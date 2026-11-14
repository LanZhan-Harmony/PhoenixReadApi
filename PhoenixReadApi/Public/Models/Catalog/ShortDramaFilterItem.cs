namespace PhoenixReadApi;

/// <summary>短剧多维筛选选项条目。</summary>
public sealed class ShortDramaFilterItem
{
    /// <summary>选项在界面上的显示名称（例如“全部”、“现代都市”）。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>向服务端请求时提交的选项值字符串。</summary>
    public string Value { get; init; } = string.Empty;
}
