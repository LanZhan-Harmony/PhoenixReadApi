using System.Text.Json.Serialization;

namespace PhoenixReadApi;

/// <summary>剧库或搜索接口返回的剧集信息。</summary>
public class Drama
{
    /// <summary>跨数据源通用的剧集 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>固定的数据源名称。</summary>
    public string Source { get; init; } = "hongguo";

    /// <summary>红果接口分配的 ID。</summary>
    public string SourceId { get; init; } = string.Empty;

    /// <summary>剧集标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>剧集名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>剧集简介。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>封面图片 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>红果接口返回的分类。</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>展示渠道。</summary>
    public string Channel { get; init; } = "红果";

    /// <summary>红果接口返回的备注。</summary>
    public string Remark { get; init; } = string.Empty;

    /// <summary>红果接口返回的评分。</summary>
    public string Score { get; init; } = string.Empty;

    /// <summary>红果接口返回的播放量。</summary>
    public string Views { get; init; } = string.Empty;

    /// <summary>红果接口返回的热度值。</summary>
    public string Heat { get; init; } = string.Empty;

    /// <summary>首次展示日期。</summary>
    public string OnlineDate { get; init; } = string.Empty;

    /// <summary>分集数量。</summary>
    public int EpisodeCount { get; init; }

    /// <summary>更新状态。</summary>
    public string ReleaseStatus { get; init; } = string.Empty;

    /// <summary>红果接口返回的标签。</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>返回最适合展示的标题。</summary>
    // 如果应用使用自己的上下文序列化公共模型，请将此计算属性排除在 JSON 之外。
    [JsonIgnore]
    public string DisplayTitle =>
        string.IsNullOrWhiteSpace(Title)
            ? (string.IsNullOrWhiteSpace(Name) ? "短剧" : Name)
            : Title;
}

