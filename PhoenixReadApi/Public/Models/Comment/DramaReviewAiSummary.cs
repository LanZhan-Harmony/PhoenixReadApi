namespace PhoenixReadApi;

/// <summary>短剧 AI 综合评价信息。</summary>
public sealed class DramaReviewAiSummary
{
    /// <summary>AI 综评唯一标识。</summary>
    public string SummaryId { get; init; } = string.Empty;

    /// <summary>AI 全文综合评述。</summary>
    public string OverallSummary { get; init; } = string.Empty;

    /// <summary>各维度细分评价列表。</summary>
    public IReadOnlyList<DramaReviewAiSummaryItem> Items { get; init; } = [];
}
