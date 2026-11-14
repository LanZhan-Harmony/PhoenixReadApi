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

    /// <summary>播放量文案（如 "120万"）。</summary>
    public string Views { get; init; } = string.Empty;

    /// <summary>热度值文案（如 "4011万热度"）。</summary>
    public string Heat { get; init; } = string.Empty;

    /// <summary>首次上架日期。</summary>
    public string OnlineDate { get; init; } = string.Empty;

    /// <summary>分集数量。</summary>
    public int EpisodeCount { get; init; }

    /// <summary>更新状态。</summary>
    public DramaReleaseStatus ReleaseStatus { get; init; } = DramaReleaseStatus.Unknown;

    /// <summary>剧集标签。</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>与本剧有关的账号及其署名关系。</summary>
    /// <remarks>
    /// 列表可能包含演员、导演、编剧、出品方等账号；出演角色和其他关系见
    /// <see cref="DramaCredit.Roles"/>。
    /// </remarks>
    public IReadOnlyList<DramaCredit> Credits { get; init; } = [];

    /// <summary>是否属于多季合集短剧。</summary>
    public bool IsMultiSeason { get; init; }

    /// <summary>在多季合集中的季序号（从 1 开始）。</summary>
    public int SeasonIndex { get; init; } = 1;

    /// <summary>多季合集 ID（若属于系列剧）。</summary>
    public string AlbumId { get; init; } = string.Empty;

    /// <summary>改编的原著小说 ID（若有）。</summary>
    public string NovelId { get; init; } = string.Empty;

    /// <summary>改编的原著小说标题（若有）。</summary>
    public string NovelTitle { get; init; } = string.Empty;

    /// <summary>总播放量数值。</summary>
    public long PlayCount { get; init; }

    /// <summary>关注人数。</summary>
    public long FollowCount { get; init; }

    /// <summary>点赞数量。</summary>
    public long LikeCount { get; init; }
}
