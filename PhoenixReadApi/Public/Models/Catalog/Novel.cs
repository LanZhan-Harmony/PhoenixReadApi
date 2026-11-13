namespace PhoenixReadApi;

/// <summary>原著小说信息。</summary>
public sealed class Novel
{
    /// <summary>小说书籍 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>小说标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>小说简称/短标题。</summary>
    public string ShortTitle { get; init; } = string.Empty;

    /// <summary>小说作者。</summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>小说简介/摘要内容。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>小说创作与连载状态枚举。</summary>
    public NovelCreationStatus Status { get; init; } = NovelCreationStatus.Unknown;

    /// <summary>连载状态描述文本（如“连载中”、“已完结”）。</summary>
    public string StatusText { get; init; } = string.Empty;

    /// <summary>小说封面图直链。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>全网累计阅读量或在读人数。</summary>
    public long ReadCount { get; init; }

    /// <summary>总章节或连载回数。</summary>
    public int SerialCount { get; init; }

    /// <summary>小说分类与特征标签。</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
