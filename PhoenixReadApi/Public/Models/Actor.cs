namespace PhoenixReadApi;

/// <summary>演员主页及其参演剧集。</summary>
public sealed class Actor
{
    /// <summary>演员 ID；通过 <see cref="PhoenixReadApiClient.GetActorAsync(string, CancellationToken)"/> 查询时与 <see cref="ProfileId"/> 相同。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>用于打开演员主页的用户 ID。</summary>
    public string ProfileId { get; init; } = string.Empty;

    /// <summary>演员姓名。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>演员简介。</summary>
    public string Introduction { get; init; } = string.Empty;

    /// <summary>演员头像 URL。</summary>
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>演员参演的剧集。</summary>
    public IReadOnlyList<Drama> Works { get; init; } = [];
}
