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
}
