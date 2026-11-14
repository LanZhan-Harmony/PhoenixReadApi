namespace PhoenixReadApi;

/// <summary>不感兴趣负反馈面板配置信息。</summary>
public sealed class DislikePanel
{
    /// <summary>卡片业务类型。</summary>
    public int CardType { get; init; }

    /// <summary>选项分组列表。</summary>
    public IReadOnlyList<DislikeGroup> Groups { get; init; } = [];
}
