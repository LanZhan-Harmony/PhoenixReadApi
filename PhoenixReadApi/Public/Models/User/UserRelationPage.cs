namespace PhoenixReadApi;

/// <summary>用户关系列表（粉丝列表或关注列表）分页数据。</summary>
public sealed class UserRelationPage
{
    /// <summary>本页用户列表。</summary>
    public IReadOnlyList<UserRelationItem> Items { get; init; } = [];

    /// <summary>下一页游标；为空或为 "0" 时表示没有更多。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>服务端指示是否还有更多数据。</summary>
    public bool HasMore { get; init; }

    /// <summary>目标用户是否开启了“隐藏粉丝/关注列表”的隐私保护（为 true 时列表将为空）。</summary>
    public bool IsPrivacyProtected { get; init; }
}
