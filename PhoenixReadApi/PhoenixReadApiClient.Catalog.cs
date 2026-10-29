namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取指定分类的一组剧集。</summary>
    /// <param name="category">
    /// 要获取的分类。使用 <see cref="PhoenixReadApiCategory.All"/> 获取全部分类，或使用真人剧、漫剧、
    /// AI 剧对应的枚举值获取单一分类。
    /// </param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按接口返回顺序排列的剧集列表；列表中的 ID 可传给 <see cref="GetDramaDetailsAsync"/>。</returns>
    /// <remarks>
    /// 分类接口可能需要多个分页请求，分页由客户端内部处理。结果是当前请求时刻可取得的列表，
    /// 不代表服务端全部剧集总数。
    /// </remarks>
    public Task<IReadOnlyList<Drama>> GetCatalogAsync(
        PhoenixReadApiCategory category = PhoenixReadApiCategory.All,
        CancellationToken cancellationToken = default
    ) => _catalog.GetAsync(category, cancellationToken);

    /// <summary>获取剧库的题材标签分类树（如热门、情节、主题、背景、角色等标签组）。</summary>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>题材标签分类分组列表。</returns>
    public Task<IReadOnlyList<TagCategoryGroup>> GetTagCategoriesAsync(
        CancellationToken cancellationToken = default
    ) => _catalog.GetTagCategoriesAsync(cancellationToken);

    /// <summary>按题材标签分页获取短剧列表。</summary>
    /// <param name="tagItemId">标签项标识（例如 "cate_261" 表示都市日常），传空时获取全部。</param>
    /// <param name="sortType">排序方式，默认 "new_sort_hot"（最热），可选 "new_sort_latest"（最新）等。</param>
    /// <param name="offset">分页偏移量，初次获取传 0。</param>
    /// <param name="count">每页条数，范围 1 到 30，默认 12。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>当前页短剧列表及分页游标状态。</returns>
    public Task<TagDramaPage> GetTagDramaPageAsync(
        string tagItemId = "",
        string sortType = "new_sort_hot",
        int offset = 0,
        int count = 12,
        CancellationToken cancellationToken = default
    ) => _catalog.GetTagDramaPageAsync(tagItemId, sortType, offset, count, cancellationToken);
}
