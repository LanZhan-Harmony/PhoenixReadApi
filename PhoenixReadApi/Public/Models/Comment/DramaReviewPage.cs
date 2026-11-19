namespace PhoenixReadApi;

/// <summary>短剧剧评分页结果，包含剧评列表、全剧综合评分、打分人数与 AI 综评。</summary>
public sealed class DramaReviewPage
{
    /// <summary>短剧 ID。</summary>
    public string DramaId { get; init; } = string.Empty;

    /// <summary>剧评列表。</summary>
    public IReadOnlyList<DramaReview> Items { get; init; } = [];

    /// <summary>全剧公信综合评分（10分制，如 9.5；若无评分则为 null）。</summary>
    public double? DramaScore { get; init; }

    /// <summary>全剧有效总评分人数。</summary>
    public long ScoreCount { get; init; }

    /// <summary>该剧的 AI 综合评价（若有）。</summary>
    public DramaReviewAiSummary? AiSummary { get; init; }

    /// <summary>下一页分页游标。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>是否还有更多剧评。</summary>
    public bool HasMore { get; init; }

    /// <summary>该短剧下的剧评总数。</summary>
    public long Total { get; init; }
}
