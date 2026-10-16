namespace PhoenixReadApi;

/// <summary>剧集中的演员信息。</summary>
public sealed class DramaActor
{
    /// <summary>演员记录 ID（接口字段 <c>celebrity_id</c>）。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>用于打开演员主页的用户 ID。</summary>
    public string ProfileId { get; init; } = string.Empty;

    /// <summary>演员姓名。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>演员简介。</summary>
    public string Introduction { get; init; } = string.Empty;

    /// <summary>演员头像 URL。</summary>
    public string AvatarUrl { get; init; } = string.Empty;

    /// <summary>饰演的角色名称。</summary>
    public string RoleName { get; init; } = string.Empty;
}
