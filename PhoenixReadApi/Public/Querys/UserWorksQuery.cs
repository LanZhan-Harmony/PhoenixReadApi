namespace PhoenixReadApi;

/// <summary>用户或演职人员主页作品查询参数。</summary>
public sealed class UserWorksQuery
{
    /// <summary>作品子分类，默认为 <see cref="UserWorkSubTab.All"/>。</summary>
    public UserWorkSubTab SubTab { get; init; } = UserWorkSubTab.All;

    /// <summary>作品排序方式，默认为 <see cref="UserWorkSort.Latest"/>。</summary>
    public UserWorkSort Sort { get; init; } = UserWorkSort.Latest;

    /// <summary>分页请求数量，默认为 18，最大为 50。</summary>
    public int Count { get; init; } = 18;

    /// <summary>分页游标，第一页可传空字符串。</summary>
    public string Cursor { get; init; } = string.Empty;
}
