namespace PhoenixReadApi;

/// <summary>剧库或搜索接口返回的剧集信息。</summary>
public sealed class Drama
{
    /// <summary>剧集 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>剧集标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>剧集简介。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>封面图片 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>剧集分类。</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>剧集备注。</summary>
    public string Remark { get; init; } = string.Empty;

    /// <summary>剧集评分。</summary>
    public string Score { get; init; } = string.Empty;

    /// <summary>播放量。</summary>
    public string Views { get; init; } = string.Empty;

    /// <summary>热度值。</summary>
    public string Heat { get; init; } = string.Empty;

    /// <summary>首次上架日期。</summary>
    public string OnlineDate { get; init; } = string.Empty;

    /// <summary>分集数量。</summary>
    public int EpisodeCount { get; init; }

    /// <summary>更新状态。</summary>
    public string ReleaseStatus { get; init; } = string.Empty;

    /// <summary>剧集标签。</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>参演演员及其饰演的角色。</summary>
    public IReadOnlyList<DramaActor> Actors { get; init; } = [];
}
