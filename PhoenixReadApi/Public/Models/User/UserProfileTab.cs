namespace PhoenixReadApi;

/// <summary>用户主页展示的内容 Tab 信息。</summary>
public sealed class UserProfileTab
{
    /// <summary>Tab 标识类型（例如作品、红果圈、收藏、点赞等）。</summary>
    public UserProfileTabType Type { get; init; } = UserProfileTabType.Unknown;

    /// <summary>Tab 显示名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>该 Tab 包含的内容总数。</summary>
    public long Total { get; init; }
}
