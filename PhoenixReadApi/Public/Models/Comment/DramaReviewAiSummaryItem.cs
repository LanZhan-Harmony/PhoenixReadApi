namespace PhoenixReadApi;

/// <summary>AI 综评维度拆解项。</summary>
public sealed class DramaReviewAiSummaryItem
{
    /// <summary>评价维度（例如 "演员"、"剧情情节"、"服化道"）。</summary>
    public string Dimension { get; init; } = string.Empty;

    /// <summary>该维度的 AI 总结评价。</summary>
    public string Summary { get; init; } = string.Empty;
}
