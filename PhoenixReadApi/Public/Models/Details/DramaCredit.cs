namespace PhoenixReadApi;

/// <summary>账号与剧集之间的署名关系。</summary>
public sealed class DramaCredit
{
    /// <summary>署名账号。</summary>
    public User User { get; init; } = new();

    /// <summary>该账号在本剧中的署名关系。</summary>
    public IReadOnlyList<DramaCreditRole> Roles { get; init; } = [];
}
