namespace PhoenixReadApi;

/// <summary>短剧多维矩阵筛选查询请求。</summary>
public sealed class ShortDramaFilterQuery
{
    /// <summary>短剧类型（真人剧、漫剧、AI剧），默认为 <see cref="ShortDramaGenre.ShortPlay"/>。</summary>
    public ShortDramaGenre Genre { get; init; } = ShortDramaGenre.ShortPlay;

    /// <summary>筛选选项映射表：键为 <see cref="ShortDramaFilterRow.Key"/>，值为选中的选项 <see cref="ShortDramaFilterOption.Value"/> 列表。</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Selections { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>分页偏移量，第一页传 0，后续传上一页返回的 <see cref="ShortDramaFilterPage.NextOffset"/>。</summary>
    public int Offset { get; init; }

    /// <summary>会话 ID（首次传 null 或空，后续传上一页返回的 SessionId）。</summary>
    public string? SessionId { get; init; }
}
