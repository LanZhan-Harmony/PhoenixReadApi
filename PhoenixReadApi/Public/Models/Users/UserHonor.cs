namespace PhoenixReadApi;

/// <summary>演职人员或创作者的成就荣誉勋章（如“10亿播放剧主演”）。</summary>
public sealed class UserHonor
{
    /// <summary>荣誉勋章名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>大图标 URL。</summary>
    public string IconUrl { get; init; } = string.Empty;

    /// <summary>小图标 URL。</summary>
    public string SmallIconUrl { get; init; } = string.Empty;
}
