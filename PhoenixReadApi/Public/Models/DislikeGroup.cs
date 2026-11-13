namespace PhoenixReadApi;

/// <summary>不感兴趣负反馈选项分组。</summary>
public sealed class DislikeGroup
{
    /// <summary>分组标题（例如“我不想看”、“内容反馈”）。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>分组图标 URL。</summary>
    public string IconUrl { get; init; } = string.Empty;

    /// <summary>包含的反馈选项列表。</summary>
    public IReadOnlyList<DislikeOption> Options { get; init; } = [];
}