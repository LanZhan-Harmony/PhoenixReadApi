namespace PhoenixReadApi;

/// <summary>单剧专属关联推荐结果（播放器/详情页更多好剧推荐）。</summary>
public sealed class DramaRelatedRecommendations
{
    /// <summary>推荐板块标题（如“相关推荐”、“更多好剧”）。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>服务端推荐会话标识。</summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>推荐的短剧列表。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];
}
