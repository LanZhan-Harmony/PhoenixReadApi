namespace PhoenixReadApi;

/// <summary>红果账号的通用身份信息。</summary>
/// <remarks>
/// 演员、导演、编剧、出品方和评论作者都使用同一种账号身份模型。
/// 账号在剧集中的职责由 <see cref="DramaCredit"/> 描述。
/// </remarks>
public sealed class User
{
    /// <summary>账号 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>账号名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>账号简介。</summary>
    public string Introduction { get; init; } = string.Empty;

    /// <summary>账号头像 URL。</summary>
    public string AvatarUrl { get; init; } = string.Empty;
}
