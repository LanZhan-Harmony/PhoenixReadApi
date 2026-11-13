namespace PhoenixReadApi;

/// <summary>账号在一部剧中的一项署名关系。</summary>
public sealed class DramaCreditRole
{
    /// <summary>署名关系类型。</summary>
    public DramaCreditRoleType Type { get; init; } = DramaCreditRoleType.Unknown;

    /// <summary>出演时饰演的角色名称。</summary>
    public string CharacterName { get; init; } = string.Empty;

    /// <summary>接口返回的原始关系文字，例如“饰 XXX”或“导演”。</summary>
    public string Label { get; init; } = string.Empty;
}
