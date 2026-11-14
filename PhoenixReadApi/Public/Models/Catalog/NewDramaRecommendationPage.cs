namespace PhoenixReadApi;

/// <summary>新剧推荐分页结果（对应红果应用新剧专区，带上新、首发标签并按受众分类）。</summary>
public sealed class NewDramaRecommendationPage
{
    /// <summary>本页短剧列表。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>是否还有更多新剧可加载。</summary>
    public bool HasMore { get; init; }

    /// <summary>下一页偏移量，作为下一次请求的 offset 参数传入。</summary>
    public int NextOffset { get; init; }

    /// <summary>会话 ID，用于在后续分页请求中保持推荐流连续性。</summary>
    public string SessionId { get; init; } = string.Empty;
}
