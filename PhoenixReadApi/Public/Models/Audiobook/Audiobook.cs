namespace PhoenixReadApi;

/// <summary>有声听书信息。</summary>
public sealed class Audiobook
{
    /// <summary>听书作品 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>有声听书资源 ID（<c>listen_book_id</c>，历史记录等接口可能单独返回）。</summary>
    public string ListenBookId { get; init; } = string.Empty;

    /// <summary>作品标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>作者及主播署名信息。</summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>作品简介。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>所属分类名称。</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>作品连载状态枚举。</summary>
    public NovelCreationStatus Status { get; init; } = NovelCreationStatus.Unknown;

    /// <summary>连载状态描述文本。</summary>
    public string StatusText { get; init; } = string.Empty;

    /// <summary>封面图片 URL（含方形有声封面优先兜底）。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>收听或阅读热度数值。</summary>
    public long ListenCount { get; init; }

    /// <summary>作品评分。</summary>
    public string Score { get; init; } = string.Empty;

    /// <summary>总集数或章节数。</summary>
    public int SerialCount { get; init; }

    /// <summary>作品标签列表。</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
